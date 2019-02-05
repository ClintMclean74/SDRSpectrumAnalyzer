/*
* Author: Clint Mclean
*
* RTLSDRDevice is a DLL for doing frequency scans using an RTL2832 based DVB dongle
* Code uses the librtlsdr library: https://github.com/steve-m/librtlsdr
* and code based on the included rtl_power.c to get frequency strength readings
*
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/


#include "rtl-sdr.h"
#include "convenience.h"
#include "stdafx.h"
#include <fstream>
#include <time.h>

#ifdef _MANAGED
#pragma managed(push, off)
#endif

#define DEFAULT_BUF_LENGTH		(1 * 16384)
#define BUFFER_DUMP			(1<<12)
#define MAXIMUM_RATE			2800000
#define MINIMUM_RATE			1000000

static rtlsdr_dev_t *dev = NULL;

static rtlsdr_dev_t *dev2 = NULL;

#include <math.h>

#define M_PI       3.14159265358979323846

bool useDB = false;

struct tuning_state
/* one per tuning range */
{
	int freq;
	int rate;
	int bin_e;
	long *avg;  /* length == 2^bin_e */
	int samples;
	int downsample;
	int downsample_passes;  /* for the recursive filter */
	double crop;
	//pthread_rwlock_t avg_lock;
	//pthread_mutex_t avg_mutex;
	/* having the iq buffer here is wasteful, but will avoid contention */
	uint8_t *buf8;
	int buf_len;
	//int *comp_fir;
	//pthread_rwlock_t buf_lock;
	//pthread_mutex_t buf_mutex;

	int totalMagnitude;
	double avgMagnitude;
};

/* 3000 is enough for 3GHz b/w worst case */
#define MAX_TUNES	3000
struct tuning_state tunesDevice1[MAX_TUNES];
struct tuning_state tunesDevice2[MAX_TUNES];
int tune_count = 0;
int boxcar = 1;
int ppm_error = 0;

int16_t* Sinewave;
double* power_table;
int N_WAVE, LOG2_N_WAVE;
int next_power;
int16_t *fft_buf;
int *window_coefs;

int interval = 10;
time_t next_tick;
time_t time_now;
time_t exit_time = 0;

#define CIC_TABLE_MAX 10
int cic_9_tables[][10] = {
	{0,},
	{9, -156,  -97, 2798, -15489, 61019, -15489, 2798,  -97, -156},
	{9, -128, -568, 5593, -24125, 74126, -24125, 5593, -568, -128},
	{9, -129, -639, 6187, -26281, 77511, -26281, 6187, -639, -129},
	{9, -122, -612, 6082, -26353, 77818, -26353, 6082, -612, -122},
	{9, -120, -602, 6015, -26269, 77757, -26269, 6015, -602, -120},
	{9, -120, -582, 5951, -26128, 77542, -26128, 5951, -582, -120},
	{9, -119, -580, 5931, -26094, 77505, -26094, 5931, -580, -119},
	{9, -119, -578, 5921, -26077, 77484, -26077, 5921, -578, -119},
	{9, -119, -577, 5917, -26067, 77473, -26067, 5917, -577, -119},
	{9, -199, -362, 5303, -25505, 77489, -25505, 5303, -362, -199},
};

int comp_fir_size = 0;
int peak_hold = 0;

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
    return TRUE;
}

#define round(x) (x > 0.0 ? floor(x + 0.5): ceil(x - 0.5))

inline int16_t FIX_MPY(int16_t a, int16_t b)
/* fixed point multiply and scale */
{
	int c = ((int)a * (int)b) >> 14;
	b = c & 0x01;
	return (c >> 1) + b;
}

int fix_fft(int16_t iq[], int m)
/* interleaved iq[], 0 <= n < 2**m, changes in place */
{
	int mr, nn, i, j, l, k, istep, n, shift;
	int16_t qr, qi, tr, ti, wr, wi;
	n = 1 << m;
	if (n > N_WAVE)
		{return -1;}
	mr = 0;
	nn = n - 1;
	/* decimation in time - re-order data */
	for (m=1; m<=nn; ++m) {
		l = n;
		do
			{l >>= 1;}
		while (mr+l > nn);
		mr = (mr & (l-1)) + l;
		if (mr <= m)
			{continue;}
		// real = 2*m, imag = 2*m+1
		tr = iq[2*m];
		iq[2*m] = iq[2*mr];
		iq[2*mr] = tr;
		ti = iq[2*m+1];
		iq[2*m+1] = iq[2*mr+1];
		iq[2*mr+1] = ti;
	}
	l = 1;
	k = LOG2_N_WAVE-1;
	while (l < n) {
		shift = 1;
		istep = l << 1;
		for (m=0; m<l; ++m) {
			j = m << k;
			wr =  Sinewave[j+N_WAVE/4];
			wi = -Sinewave[j];
			if (shift) {
				wr >>= 1; wi >>= 1;}
			for (i=m; i<n; i+=istep) {
				j = i + l;
				tr = FIX_MPY(wr,iq[2*j]) - FIX_MPY(wi,iq[2*j+1]);
				ti = FIX_MPY(wr,iq[2*j+1]) + FIX_MPY(wi,iq[2*j]);
				qr = iq[2*i];
				qi = iq[2*i+1];
				if (shift) {
					qr >>= 1; qi >>= 1;}
				iq[2*j] = qr - tr;
				iq[2*j+1] = qi - ti;
				iq[2*i] = qr + tr;
				iq[2*i+1] = qi + ti;
			}
		}
		--k;
		l = istep;
	}
	return 0;
}

double rectangle(int i, int length)
{
	return 1.0;
}

double log2(double n)
{
	return log(n) / log(2.0);
}

void sine_table(int size)
{
	int i;
	double d;
	LOG2_N_WAVE = size;
	N_WAVE = 1 << LOG2_N_WAVE;
	Sinewave = (int16_t *) malloc(sizeof(int16_t) * N_WAVE*3/4);
	power_table = (double *) malloc(sizeof(double) * N_WAVE);
	for (i=0; i<N_WAVE*3/4; i++)
	{
		d = (double)i * 2.0 * M_PI / N_WAVE;
		Sinewave[i] = (int)round(32767*sin(d));
		//printf("%i\n", Sinewave[i]);
	}
}

void buildTunesArray(struct tuning_state *tunes, int lower, int bw_seen, int bw_used, int bin_e, int buf_len, double crop, int downsample, int downsample_passes)
{
	struct tuning_state *ts;

	for (int i = 0; i<tune_count; i++) {
		ts = &tunes[i];
		ts->freq = lower + i*bw_seen + bw_seen / 2;
		ts->rate = bw_used;
		ts->bin_e = bin_e;
		ts->samples = 0;
		ts->crop = crop;
		ts->downsample = downsample;
		ts->downsample_passes = downsample_passes;
		ts->avg = (long*)malloc((1 << bin_e) * sizeof(long));
		if (!ts->avg) {
			fprintf(stderr, "Error: malloc.\n");
			exit(1);
		}
		for (int j = 0; j<(1 << bin_e); j++) {
			ts->avg[j] = 0L;
		}
		ts->buf8 = (uint8_t*)malloc(buf_len * sizeof(uint8_t));
		if (!ts->buf8) {
			fprintf(stderr, "Error: malloc.\n");
			exit(1);
		}
		ts->buf_len = buf_len;
	}
}

void frequency_range(unsigned int startFrequency, unsigned int endFrequency, unsigned int stepSize, struct tuning_state *tunes, unsigned int maxSamplingRate = 0)
/* flesh out the tunes[] for scanning */
// do we want the fewest ranges (easy) or the fewest bins (harder)?
{	
	int i, j, upper, lower, max_size, bw_seen, bw_used, bin_e, buf_len;
	int downsample, downsample_passes;
	double bin_size;
	struct tuning_state *ts;

	double crop = 0.0;
	
	lower = startFrequency;
	upper = endFrequency;
	max_size = stepSize;	

	if (!maxSamplingRate)
		maxSamplingRate = MAXIMUM_RATE;
	
	downsample = 1;
	downsample_passes = 0;
	/* evenly sized ranges, as close to maxSamplingRate as possible */
	// todo, replace loop with algebra
	for (i=1; i<1500; i++) {
		bw_seen = (upper - lower) / i;
		bw_used = (int)((double)(bw_seen) / (1.0 - crop));
		if (bw_used > maxSamplingRate) {
			continue;}
		tune_count = i;
		break;
	}
	/* unless small bandwidth */
	if (bw_used < MINIMUM_RATE) {
		tune_count = 1;
		downsample = maxSamplingRate / bw_used;
		bw_used = bw_used * downsample;
	}
	if (!boxcar && downsample > 1) {
		downsample_passes = (int)log2(downsample);
		downsample = 1 << downsample_passes;
		bw_used = (int)((double)(bw_seen * downsample) / (1.0 - crop));
	}
	/* number of bins is power-of-two, bin size is under limit */
	// todo, replace loop with log2
	for (i=1; i<=21; i++) {
		bin_e = i;
		bin_size = (double)bw_used / (double)((1<<i) * downsample);
		if (bin_size <= (double)max_size) {
			break;}
	}
	/* unless giant bins */
	if (max_size >= MINIMUM_RATE) {
		bw_seen = max_size;
		bw_used = max_size;
		tune_count = (upper - lower) / bw_seen;
		bin_e = 0;
		crop = 0;
	}
	if (tune_count > MAX_TUNES) {
		fprintf(stderr, "Error: bandwidth too wide.\n");
		exit(1);
	}
	buf_len = 2 * (1<<bin_e) * downsample;
	if (buf_len < DEFAULT_BUF_LENGTH) {
		buf_len = DEFAULT_BUF_LENGTH;
	}
	buildTunesArray(tunes, lower, bw_seen, bw_used, bin_e, buf_len, crop, downsample, downsample_passes);
	/* report */
	fprintf(stderr, "Number of frequency hops: %i\n", tune_count);
	fprintf(stderr, "Dongle bandwidth: %iHz\n", bw_used);
	fprintf(stderr, "Downsampling by: %ix\n", downsample);
	fprintf(stderr, "Cropping by: %0.2f%%\n", crop*100);
	fprintf(stderr, "Total FFT bins: %i\n", tune_count * (1<<bin_e));
	fprintf(stderr, "Logged FFT bins: %i\n", \
	  (int)((double)(tune_count * (1<<bin_e)) * (1.0-crop)));
	fprintf(stderr, "FFT bin size: %0.2fHz\n", bin_size);
	fprintf(stderr, "Buffer size: %i bytes (%0.2fms)\n", buf_len, 1000 * 0.5 * (float)buf_len / (float)bw_used);	
}


rtlsdr_dev_t* InitializeDevice(int dev_index, unsigned int startFrequency, unsigned int endFrequency, unsigned int stepSize, rtlsdr_dev_t *device, unsigned int maxSamplingRate)
{
	double(*window_fn)(int, int) = rectangle;

	if (device == NULL)
	{
		int r = rtlsdr_open(&device, (uint32_t)dev_index);		

		if (r < 0) {
			//fprintf(stderr, "Failed to open rtlsdr device #%d.\n", dev_index);		
			return NULL;
		}
	}

	verbose_auto_gain(device);

	////verbose_gain_set(device, 800);

	verbose_ppm_set(device, ppm_error);

	verbose_reset_buffer(device);

	struct tuning_state *tunes;

	if (dev_index == 0)
		tunes = tunesDevice1;
	else
		tunes = tunesDevice2;

	frequency_range(startFrequency, endFrequency, stepSize, tunes, maxSamplingRate);

	rtlsdr_set_sample_rate(device, (uint32_t)tunes[0].rate);

	sine_table(tunes[0].bin_e);
	next_tick = time(NULL) + interval;
	if (exit_time) {
		exit_time = time(NULL) + exit_time;
	}
	fft_buf = (int16_t *)malloc(tunes[0].buf_len * sizeof(int16_t));
	int length = 1 << tunes[0].bin_e;
	window_coefs = (int *)malloc(length * sizeof(int));

	for (int i = 0; i < length; i++) {
		window_coefs[i] = (int)(256 * window_fn(i, length));
	}

	return device;
}

int GetConnectedDevicesCount()
{
	verbose_device_search("0");
	return rtlsdr_get_device_count();
}

int Initialize(unsigned int startFrequency, unsigned int endFrequency, unsigned int stepSize, unsigned int maxSamplingRate)
{
	int dev_index;

	int devCount = 0;

	dev_index = verbose_device_search("0");

	if (dev_index > -1)
		dev = InitializeDevice(dev_index, startFrequency, endFrequency, stepSize, dev, maxSamplingRate);

	dev_index = verbose_device_search("1");

	if (dev_index > -1)
		dev2 = InitializeDevice(dev_index, startFrequency, endFrequency, stepSize, dev2, maxSamplingRate);

	if (dev2 == NULL)
	{
		dev_index = verbose_device_search("2");

		if (dev_index > -1)
			dev2 = InitializeDevice(dev_index, startFrequency, endFrequency, stepSize, dev2, maxSamplingRate);
	}

	if (dev)
	{
		devCount++;
	}

	if (dev2)
	{
		devCount++;
	}

	return devCount;
}

unsigned int get_device_count()
{
	verbose_device_search("0");
	return rtlsdr_get_device_count();
}

const char* get_device_name(unsigned int index)
{
	return rtlsdr_get_device_name(index);	
}

void fifth_order(int16_t *data, int length)
/* for half of interleaved data */
{
	int i;
	int a, b, c, d, e, f;
	a = data[0];
	b = data[2];
	c = data[4];
	d = data[6];
	e = data[8];
	f = data[10];
	/* a downsample should improve resolution, so don't fully shift */
	/* ease in instead of being stateful */
	data[0] = ((a+b)*10 + (c+d)*5 + d + f) >> 4;
	data[2] = ((b+c)*10 + (a+d)*5 + e + f) >> 4;
	data[4] = (a + (b+e)*5 + (c+d)*10 + f) >> 4;
	for (i=12; i<length; i+=4) {
		a = c;
		b = d;
		c = e;
		d = f;
		e = data[i-2];
		f = data[i];
		data[i/2] = (a + (b+e)*5 + (c+d)*10 + f) >> 4;
	}
}

void generic_fir(int16_t *data, int length, int *fir)
/* Okay, not at all generic.  Assumes length 9, fix that eventually. */
{
	int d, temp, sum;
	int hist[9] = {0,};
	/* cheat on the beginning, let it go unfiltered */
	for (d=0; d<18; d+=2) {
		hist[d/2] = data[d];
	}
	for (d=18; d<length; d+=2) {
		temp = data[d];
		sum = 0;
		sum += (hist[0] + hist[8]) * fir[1];
		sum += (hist[1] + hist[7]) * fir[2];
		sum += (hist[2] + hist[6]) * fir[3];
		sum += (hist[3] + hist[5]) * fir[4];
		sum +=            hist[4]  * fir[5];
		data[d] = (int16_t)(sum >> 15) ;
		hist[0] = hist[1];
		hist[1] = hist[2];
		hist[2] = hist[3];
		hist[3] = hist[4];
		hist[4] = hist[5];
		hist[5] = hist[6];
		hist[6] = hist[7];
		hist[7] = hist[8];
		hist[8] = temp;
	}
}

void remove_dc(int16_t *data, int length)
/* works on interleaved data */
{
	int i;
	int16_t ave;
	long sum = 0L;
	for (i=0; i < length; i+=2) {
		sum += data[i];
	}
	ave = (int16_t)(sum / (long)(length));
	if (ave == 0) {
		return;}
	for (i=0; i < length; i+=2) {
		data[i] -= ave;
	}
}

void downsample_iq(int16_t *data, int length)
{
	fifth_order(data, length);
	//remove_dc(data, length);
	fifth_order(data+1, length-1);
	//remove_dc(data+1, length-1);
}

void rms_power(struct tuning_state *ts)
/* for bins between 1MHz and 2MHz */
{
	int i, s;
	uint8_t *buf = ts->buf8;
	int buf_len = ts->buf_len;
	long p, t;
	double dc, err;

	p = t = 0L;
	for (i=0; i<buf_len; i++) {
		s = (int)buf[i] - 127;
		t += (long)s;
		p += (long)(s * s);
	}
	/* correct for dc offset in squares */
	dc = (double)t / (double)buf_len;
	err = t * 2 * dc - dc * dc * buf_len;
	p -= (long)round(err);

	if (!peak_hold) {
		ts->avg[0] += p;
	} else {
		ts->avg[0] = max(ts->avg[0], p);
	}
	ts->samples += 1;
}


void retune(rtlsdr_dev_t *d, int freq)
{
	uint8_t dump[BUFFER_DUMP];
	int n_read;
	rtlsdr_set_center_freq(d, (uint32_t)freq);
	/* wait for settling and flush buffer */
	Sleep(5);
	rtlsdr_read_sync(d, &dump, BUFFER_DUMP, &n_read);
	if (n_read != BUFFER_DUMP) {
		fprintf(stderr, "Error: bad retune.\n");}
}

long real_conj(int16_t real, int16_t imag)
/* real(n * conj(n)) */
{
	return ((long)real*(long)real + (long)imag*(long)imag);
}

double GetTotalADCMagnitude(tuning_state *ts)
{
	int iValue, qValue;
	int totalMagnitude = 0;

	int samples = ts->buf_len / 2;

	for (int i = 0; i < ts->buf_len; i += 2)
	{
		iValue = ts->buf8[i] - 127;
		qValue = ts->buf8[i + 1] - 127;

		totalMagnitude += iValue*iValue + qValue*qValue;
	}

	ts->totalMagnitude = totalMagnitude;
	ts->avgMagnitude = (double)totalMagnitude / samples;

	return ts->totalMagnitude;
}

double GetAverageMagnitude(tuning_state *ts)
{	
	int iValue, qValue;
	int totalMagnitude = 0;

	int samples = ts->buf_len / 2;

	for (int i = 0; i < ts->buf_len; i+=2)
	{
		iValue = ts->buf8[i]-127;
		qValue = ts->buf8[i+1] - 127;

		totalMagnitude += iValue*iValue + qValue*qValue;
	}

	ts->totalMagnitude = totalMagnitude;
	ts->avgMagnitude = (double) totalMagnitude / samples;

	return ts->avgMagnitude;
}


double GetTotalADCValue(int deviceIndex)
{
	rtlsdr_dev_t *device;

	if (deviceIndex == 0)
		device = dev;
	else
		device = dev2;

	double totalADCValue = 0;

	int i, j, j2, f, n_read, offset, bin_e, bin_len, buf_len, ds, ds_p;
	int32_t w;
	struct tuning_state *ts;

	bin_e = tunesDevice1[0].bin_e;
	bin_len = 1 << bin_e;
	buf_len = tunesDevice1[0].buf_len;

	for (i = 0; i<tune_count; i++)
	{
		ts = &tunesDevice1[i];
		f = (int)rtlsdr_get_center_freq(device);
		
		if (f != ts->freq)
		{
			retune(device, ts->freq);
		}

		rtlsdr_read_sync(device, ts->buf8, buf_len, &n_read);		

		if (n_read != buf_len)
		{
			fprintf(stderr, "Error: dropped samples.\n");
		}

		totalADCValue += GetTotalADCMagnitude(ts);
	}

	return totalADCValue;
}

void prepForFFT(struct tuning_state *ts)
{
	int i, j, j2, f, n_read, offset, bin_e, bin_len, buf_len, ds, ds_p;
	int32_t w;

	bin_e = ts[0].bin_e;
	bin_len = 1 << bin_e;
	buf_len = ts[0].buf_len;

	/* prep for fft */
	for (j = 0; j<buf_len; j++) {
		fft_buf[j] = (int16_t)ts->buf8[j] - 127;
	}
	ds = ts->downsample;
	ds_p = ts->downsample_passes;
	if (boxcar && ds > 1) {
		j = 2, j2 = 0;
		while (j < buf_len) {
			fft_buf[j2] += fft_buf[j];
			fft_buf[j2 + 1] += fft_buf[j + 1];
			fft_buf[j] = 0;
			fft_buf[j + 1] = 0;
			j += 2;
			if (j % (ds * 2) == 0) {
				j2 += 2;
			}
		}
	}
	else if (ds_p) {  /* recursive */
		for (j = 0; j < ds_p; j++) {
			downsample_iq(fft_buf, buf_len >> j);
		}
		/* droop compensation */
		if (comp_fir_size == 9 && ds_p <= CIC_TABLE_MAX) {
			generic_fir(fft_buf, buf_len >> j, cic_9_tables[ds_p]);
			generic_fir(fft_buf + 1, (buf_len >> j) - 1, cic_9_tables[ds_p]);
		}
	}
	remove_dc(fft_buf, buf_len / ds);
	remove_dc(fft_buf + 1, (buf_len / ds) - 1);
	/* window function and fft */
	for (offset = 0; offset<(buf_len / ds); offset += (2 * bin_len)) {
		// todo, let rect skip this
		for (j = 0; j<bin_len; j++) {
			w = (int32_t)fft_buf[offset + j * 2];
			w *= (int32_t)(window_coefs[j]);
			//w /= (int32_t)(ds);
			fft_buf[offset + j * 2] = (int16_t)w;
			w = (int32_t)fft_buf[offset + j * 2 + 1];
			w *= (int32_t)(window_coefs[j]);
			//w /= (int32_t)(ds);
			fft_buf[offset + j * 2 + 1] = (int16_t)w;
		}
		fix_fft(fft_buf + offset, bin_e);
		if (!peak_hold) {
			for (j = 0; j<bin_len; j++) {
				ts->avg[j] += real_conj(fft_buf[offset + j * 2], fft_buf[offset + j * 2 + 1]);
			}
		}
		else {
			for (j = 0; j<bin_len; j++) {
				ts->avg[j] = max(real_conj(fft_buf[offset + j * 2], fft_buf[offset + j * 2 + 1]), ts->avg[j]);
			}
		}
		ts->samples += ds;
	}
}

void ScanTune(struct tuning_state *ts, rtlsdr_dev_t *device, bool usetotalADCMagnitude = false, unsigned int scanCount = 1)
{
	int j, j2, f, offset, n_read, bin_e, bin_len, buf_len, ds, ds_p;
	int32_t w;	
	bin_e = tunesDevice1[0].bin_e;
	bin_len = 1 << bin_e;
	buf_len = tunesDevice1[0].buf_len;

	f = (int)rtlsdr_get_center_freq(device);
	if (f != ts->freq) {
		retune(device, ts->freq);
	}

	double avgTotalADCMagnitude = 0;

	for (int j = 0; j < scanCount; j++)
	{
		rtlsdr_read_sync(device, ts->buf8, buf_len, &n_read);

		if (usetotalADCMagnitude)
		{
			GetTotalADCMagnitude(ts);

			avgTotalADCMagnitude += ts->totalMagnitude;
		}
	}

	if (usetotalADCMagnitude)
		avgTotalADCMagnitude /= scanCount;

	ts->totalMagnitude = avgTotalADCMagnitude;

	if (n_read != buf_len) {
		fprintf(stderr, "Error: dropped samples.\n");
	}
	/* rms */
	if (bin_len == 1) {
		rms_power(ts);
		return;
	}

	if (!usetotalADCMagnitude)
		prepForFFT(ts);
}

void scanner(int deviceIndex, double rangeSamples, bool usetotalADCMagnitude = false, unsigned int scanCount = 1)
{
	rtlsdr_dev_t *device;

	if (deviceIndex == 0)
		device = dev;
	else
		device = dev2;	

	int sampleGap = round(tune_count / rangeSamples);

	for (int i = 0; i < tune_count; i++)
	{		
		if (i%sampleGap == 0)
		{
			ScanTune(&tunesDevice1[i], device, usetotalADCMagnitude, scanCount);
		}
		else
		{
			tunesDevice1[i] = tunesDevice1[i-1];
		}
	}
}


void devicesScanner()
{	
	int i, j, j2, f, n_read, offset, bin_e, bin_len, buf_len, ds, ds_p;
	int32_t w;
	struct tuning_state *ts1, *ts2;
	bin_e = tunesDevice1[0].bin_e;
	bin_len = 1 << bin_e;
	buf_len = tunesDevice1[0].buf_len;
	for (i = 0; i<tune_count; i++)
	{
		ts1 = &tunesDevice1[i];
		f = (int)rtlsdr_get_center_freq(dev);
		if (f != ts1->freq) {
			retune(dev, ts1->freq);
		}
		rtlsdr_read_sync(dev, ts1->buf8, buf_len, &n_read);

		ts2 = &tunesDevice2[i];
		f = (int)rtlsdr_get_center_freq(dev2);
		if (f != ts2->freq) {
			retune(dev2, ts2->freq);
		}
		rtlsdr_read_sync(dev2, ts2->buf8, buf_len, &n_read);


		GetAverageMagnitude(ts1);

		GetAverageMagnitude(ts2);

		if (n_read != buf_len) {
			fprintf(stderr, "Error: dropped samples.\n");
		}
		/* rms */
		if (bin_len == 1) {
			rms_power(ts1);
			rms_power(ts2);
			continue;
		}		

		prepForFFT(ts1);
		prepForFFT(ts2);
	}
}

double ScanAndGetTotalADCMagnitudeForFrequency(unsigned int frequency)
{
	tuning_state *ts = new tuning_state();

	memcpy(ts, &tunesDevice1[0], sizeof(tuning_state));

	ts->freq = frequency;

	ScanTune(ts, dev, true, 1);

	return ts->totalMagnitude;
}


void csv_dbm(struct tuning_state *ts, float* buffer, long offset)
{
	int i, len, ds, i1, i2, bw2, bin_count;
	long tmp;
	double dbm;
	bool usingDB = useDB;

	len = 1 << ts->bin_e;
	ds = ts->downsample;
	/* fix FFT stuff quirks */
	if (ts->bin_e > 0) {
		/* nuke DC component (not effective for all windows) */
		ts->avg[0] = ts->avg[1];
		/* FFT is translated by 180 degrees */
		for (i=0; i<len/2; i++) {
			tmp = ts->avg[i];
			ts->avg[i] = ts->avg[i+len/2];
			ts->avg[i+len/2] = tmp;
		}
	}
	/* Hz low, Hz high, Hz step, samples, dbm, dbm, ... */
	bin_count = (int)((double)len * (1.0 - ts->crop));
	bw2 = (int)(((double)ts->rate * (double)bin_count) / (len * 2 * ds));	
	// something seems off with the dbm math
	i1 = 0 + (int)((double)len * ts->crop * 0.5);
	i2 = (len-1) - (int)((double)len * ts->crop * 0.5);
	
	int binCount = 0;		

	float* bufferPtr = &buffer[offset];
	
	for (i=i1; i<=i2; i++)
	{
		dbm  = (double)ts->avg[i];

		if (usingDB)
		{
			dbm /= (double)ts->rate;
			dbm /= (double)ts->samples;
			dbm  = 10 * log10(dbm);
		}

		bufferPtr[binCount++] = dbm;
	}
	
	for (i=0; i<len; i++) {
		ts->avg[i] = 0L;
	}
	ts->samples = 0;	
}

unsigned int GetBufferSize()
{
	long length=0;

	for (int i=0; i<tune_count; i++)			
		length += 1 << tunesDevice1[i].bin_e;

	return length;
}

void GetBins(float *buffer, int deviceIndex, double rangeSamplingPercentage, bool usetotalADCMagnitude = false, unsigned int scanCount = 1)
{
	scanCount = 1;

	double rangeSamples = rangeSamplingPercentage / 100 * tune_count;

	if (rangeSamples < 3)
		rangeSamples = tune_count;

	scanner(deviceIndex, rangeSamples, usetotalADCMagnitude, scanCount);

	long length = 0;
	long totalBufferLength = 0;
	if (!usetotalADCMagnitude)
	{
		for (int i = 0; i < tune_count; i++) {
			csv_dbm(&tunesDevice1[i], buffer, i*length);
			length = 1 << tunesDevice1[i].bin_e;
		}
	}
	else
	{
		for (int i = 0; i < tune_count; i++)
		{		
			length = 1 << tunesDevice1[i].bin_e;

			for (int j = 0; j < length; j++)
				buffer[totalBufferLength++] = tunesDevice1[i].totalMagnitude;
		}
	}
}


void GetBinsForDevices(float *buffer1, float *buffer2, int deviceIndex1, int deviceIndex2)
{
	devicesScanner();

	long length = 0;

	for (int i = 0; i<tune_count; i++) {
		csv_dbm(&tunesDevice1[i], buffer1, i*length);
		length = 1 << tunesDevice1[i].bin_e;
	}

	length = 0;

	for (int i = 0; i<tune_count; i++) {
		csv_dbm(&tunesDevice2[i], buffer2, i*length);
		length = 1 << tunesDevice2[i].bin_e;
	}
}


int GetTotalMagnitude()
{
	int totalMagnitude = 0;

	for (int i = 0; i<tune_count; i++)
	{
		totalMagnitude += tunesDevice1[i].totalMagnitude;		
	}

	return totalMagnitude;
}


double GetAvgMagnitude()
{
	double avgMagnitude = 0;

	for (int i = 0; i<tune_count; i++)
	{
		avgMagnitude += tunesDevice1[i].avgMagnitude;
	}

	return avgMagnitude/tune_count;
}


int SetUseDB(int value)
{
	useDB = value;

	return 1;
}


#ifdef _MANAGED
#pragma managed(pop)
#endif


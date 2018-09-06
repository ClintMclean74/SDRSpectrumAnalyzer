using System;
using System.IO;

namespace RTLSpectrumAnalyzerGUI
{
    public class Gradient
    {
        public double strength;
        public double transitionGradientIncrease;
        public double stackedFrames;

        public double[] divisions;

        public const int divisionsCount = 20;

        public Gradient(double strength, double stackedFrames, double[] divisions)
        {
            ////this.divisions = new double[divisions.Length];

            this.divisions = (double[])divisions.Clone();

            for (int i = 0; i < this.divisions.Length; i++)
            {
                this.strength += this.divisions[i];
            }

            this.strength /= this.divisions.Length;

            this.stackedFrames = stackedFrames;
        }

        public double LargestIncrease(double[] data)
        {
            double dif, maxChange = double.NaN;

            int maxIndex = -1;

            for (int i = 1; i < data.Length; i++)
            {
                dif = data[i] - data[i - 1];

                if (dif > maxChange || double.IsNaN(maxChange))
                {
                    maxChange = dif;
                    maxIndex = i;
                }
            }

            if (maxIndex > -1)
            {
                return maxChange;
            }

            return 0;
        }

        public double LargestIncreaseDecreaseChange(double[] data)
        {
            double totalIncreasing = 0;
            double totalDecreasing = 0;

            bool increasing = false;

            double dif, change, maxChange = double.NaN;

            int startIncIndex = -1, startDecIndex = -1, maxIndex = -1;

            for (int i = 1; i < data.Length; i++)
            {
                dif = data[i] - data[i - 1];

                if (dif > 0)
                {
                    if (!increasing)
                    {
                        if (startIncIndex > -1 && startDecIndex > -1)
                        {
                            change = totalIncreasing - totalDecreasing;

                            change = change * (data.Length / 4 - Math.Abs(data.Length * 7/20 - startDecIndex));

                            if (change > maxChange || double.IsNaN(maxChange))
                            {
                                maxChange = change;
                                /////maxIndex = startIncIndex;
                                maxIndex = startDecIndex;
                            }

                            startIncIndex = -1;
                            startDecIndex = -1;
                        }


                        ////if (data[i - 1] >= 0)
                        {
                            startIncIndex = i;

                            totalIncreasing = dif;

                            totalDecreasing = 0;
                        }
                    }
                    else
                        totalIncreasing += dif;

                    increasing = true;
                }
                else
                    if (dif <= 0)
                {
                    if (increasing)////&& data[i - 1] >= 0)
                    {
                        totalDecreasing = dif;
                        startDecIndex = i;
                    }
                    else
                        totalDecreasing += dif;

                    increasing = false;
                }
            }

            if (maxIndex > -1)
            {
                return maxChange;
            }

            return 0;
        }

        public double CalculateTransitionGradient()
        {
            double[] values = divisions;
            /////////double[] values = SignalDataUtilities.Reduce(divisions, Gradient.divisionsCount);

            double total = 0;

            int i;

            for (i = 0; i < (int)(values.Length / 2); i++)
            {

                total -= values[i];
            }

            for (; i < values.Length; i++)
            {
                total += values[i];
            }


            return total;
            

            /*////double reradiatedChange = LargestIncreaseDecreaseChange(values);


            double secondQuarter = 0, thirdAnd4Quarter = 0;

            int quarterLength = (int)(values.Length / 4);

            int i;

            for (i = quarterLength; i < quarterLength*2; i++)
            {
                ////firstThird += Math.Abs(values[i]);

                secondQuarter += Math.Abs(values[i]);
                ////secondQuarter += values[i];
            }

            secondQuarter /= quarterLength;


            for (; i < values.Length; i++)
            {
                ////firstThird += Math.Abs(values[i]);

                thirdAnd4Quarter += Math.Abs(values[i]);
                ////thirdAnd4Quarter += values[i];
            }

            thirdAnd4Quarter /= quarterLength;

            return thirdAnd4Quarter - secondQuarter;

/*//////
            /*/////////reradiatedChange = values[2] - values[3];

            reradiatedChange -= (Math.Abs(values[0]) + Math.Abs(values[1]) + Math.Abs(values[4]));
            */
            

            /*////double firstThird = 0, secondThird = 0, lastThird = 0;

            int thirdLength = (int)(values.Length / 3);

            int i;

            for (i = 0; i < thirdLength; i++)
            {
                ////firstThird += Math.Abs(values[i]);
                firstThird += values[i];
            }

            firstThird /= thirdLength;


            for (; i < thirdLength*2; i++)
            {
                ////firstThird += Math.Abs(values[i]);
                secondThird += values[i];
            }

            secondThird /= thirdLength;

            for (i = values.Length - thirdLength; i < values.Length; i++)
            {
                ////lastThird += Math.Abs(values[i]);
                lastThird += values[i];
            }

            lastThird /= thirdLength;

            return lastThird - secondThird;
            */

            ////reradiatedChange -= (Math.Abs(firstThird) + Math.Abs(lastThird));

            /////////reradiatedChange = reradiatedChange - (firstThird + lastThird);

            /////////return reradiatedChange + 100000;
            ////return LargestIncrease(values);            
        }

        public double CalculateTransitionGradient2()
        {
            double gradientTotals = 0;
            
            for (int i = 0; i < this.divisions.Length; i++)
            {
                gradientTotals += this.divisions[i];                    
            }

            double width = this.divisions.Length * 0.3;

            double centralStrength = 0;

            uint centralRegionCount = 0;

            for (int i = (int)(this.divisions.Length / 2 - width * 2 / 3); i < this.divisions.Length / 2 + width / 3; i++)
            {
                if (this.divisions[i] > 0)
                {
                    centralStrength += this.divisions[i];

                    centralRegionCount++;
                }
            }


            this.transitionGradientIncrease = Math.Round(((centralStrength / gradientTotals)) * 100, 2);

            return centralStrength;

            ////return 10;

            ////return this.transitionGradientIncrease;


            /*////double increasingGradientTotals = 0;


            int count = 0;
            for (int i = 0; i < this.divisions.Length; i++)
            {
                if (this.divisions[i] > 0)
                {
                    increasingGradientTotals += this.divisions[i];
                    count++;
                }
            }

            if (increasingGradientTotals == 0)
                increasingGradientTotals = 0.00001;


            double width = this.divisions.Length * 0.3;

            double centralStrength=0;

            uint centralRegionCount = 0;

            ////for (int i = (int) (this.divisions.Length/2 - width/2); i < this.divisions.Length/2 + width/2; i++)
            for (int i = (int)(this.divisions.Length / 2 - width*2/3); i < this.divisions.Length / 2 + width/3; i++)
            {
                if (this.divisions[i] > 0)
                {
                    centralStrength += this.divisions[i];

                    centralRegionCount++;
                }
            }



            this.transitionGradientIncrease = Math.Round(((centralStrength / increasingGradientTotals)) * 100, 2);


            if (count>0)
                this.transitionGradientIncrease = (this.transitionGradientIncrease / ((float)100 / count)) * 100;

            /*////if (this.strength < 0)
/*                this.transitionGradientIncrease *= -1;
                */

            ////this.transitionGradientIncrease = (this.transitionGradientIncrease / (100 / count)) * 100;                      

            /////////this.transitionGradientIncrease = (this.transitionGradientIncrease / (100 / this.divisions.Length)) * 100;


            ////this.transitionGradientIncrease = (this.transitionGradientIncrease / (100 * (float) centralRegionCount/ this.divisions.Length)) * 100;


            ////this.strength /= this.divisions.Length;

            /*////this.transitionGradientIncrease -= 100;

            if (this.transitionGradientIncrease < 0 && centralStrength < 0)
                this.transitionGradientIncrease *= -1;
                */


            ////return this.transitionGradientIncrease * centralStrength;

            /*////double avg1stAnd2ndGradient = (divisions[0] + divisions[1]) / 2;

            double avg4And5thGradient = (divisions[3] + divisions[4]) / 2;           

            double thirdFifthvs1245 = ((divisions[2] - avg1stAnd2ndGradient) + (divisions[2] - avg4And5thGradient)) / 2;

            return Math.Round(thirdFifthvs1245, 2);
            */


            /*////double avg1stAnd2ndGradient = (divisions[0] + divisions[1]) / 2;

            double avg345thGradient = (divisions[2] + divisions[3] + divisions[4]) / 3;

            double third45vs12 = avg345thGradient - avg1stAnd2ndGradient;

            return third45vs12;
            */


            /*////double avg1stAnd2ndGradient = (divisions[0] + divisions[1]) / 2;

            double avg234thGradient = (divisions[1] + divisions[2] + divisions[3]) / 3;            

            double second34vs15 = ((avg234thGradient - divisions[0]) + (avg234thGradient - divisions[4])) / 2;

            return second34vs15;
            */


            ////return Math.Round(thirdFifthvs1245, 2);
            /*////double min = Double.NaN;

            for (int i = 0; i < this.divisions.Length; i++)
            {
                if (this.divisions[i]<min || double.IsNaN(min))
                {
                    min = this.divisions[i];
                }
            }

            min = Math.Abs(min);

            double[] fifths2 = new double[5];

            for (int i = 0; i < this.divisions.Length; i++)
            {
                fifths2[i] = divisions[i] + min;                
            }



            double avg1stAnd2ndGradient = (fifths2[0] + fifths2[1]) / 2;

            double avg4And5thGradient = (fifths2[3] + fifths2[4]) / 2;

            ////double thirdFifthvs1245 = (fifths2[2] / avg1stAnd2ndGradient + fifths2[2] / avg4And5thGradient) / 2;

            double thirdFifthvs1245 = ((fifths2[2] - avg1stAnd2ndGradient) + (fifths2[2] - avg4And5thGradient)) / 2;

            if (avg1stAnd2ndGradient == 0 || avg4And5thGradient == 0)
            {
                if (avg1stAnd2ndGradient != 0)
                    return fifths2[2] / avg1stAnd2ndGradient;
                else
                if (avg4And5thGradient != 0)
                    return fifths2[2] / avg4And5thGradient;
                else
                    return fifths2[3];
            }
            else            
                return Math.Round(thirdFifthvs1245 * 100, 2);                                
            */
        }

        public void SaveData(BinaryWriter writer)
        {
            writer.Write(strength);

            writer.Write(stackedFrames);            
        }

        public void LoadData(BinaryReader reader)
        {
            strength = reader.ReadDouble();

            stackedFrames = reader.ReadDouble();
        }
    }    

    public class GradientArray
    {
        public long time;
        public Gradient[] gradientArray;

        public double average;

        public GradientArray(long size)
        {
            gradientArray = new Gradient[size];
        }

        public double CalculateAverage()
        {
            average = 0;

            for (int j = 0; j < gradientArray.Length; j++)
            {
                average += gradientArray[j].strength;
            }

            average /= gradientArray.Length;

            return average;
        }

        public void SaveData(BinaryWriter writer)
        {            
            writer.Write((UInt32)time);

            writer.Write((UInt32)gradientArray.Length);

            for (int j = 0; j < gradientArray.Length; j++)
            {
                gradientArray[j].SaveData(writer);                
            }
        }

        public void LoadData(BinaryReader reader)
        {
            time = reader.ReadUInt32();
            
            uint length  = reader.ReadUInt32();

            for (int j = 0; j < gradientArray.Length; j++)
            {
                gradientArray[j] = new Gradient(0, 0, new double[5]);
                gradientArray[j].LoadData(reader);
            }
        }
    }
}

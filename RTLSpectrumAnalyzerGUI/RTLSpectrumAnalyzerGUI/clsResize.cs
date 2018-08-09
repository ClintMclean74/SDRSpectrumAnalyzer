using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


public class clsResize
{
    private List<_addInfo> _arr_control_storage;
    private bool showRowHeader = false;

    public clsResize(Form _form_)
    {
        _arr_control_storage = new List<_addInfo>();
        form = _form_; //the calling form
                       //   _formSize = _form_.ClientSize; //Save initial form size
                       //   _fontsize = _form_.Font.Size; //Font size
    }
    private float _fontsize { get; set; }
    private System.Drawing.Size _formSize { get; set; }
    private Form form { get; set; }

    public void SetFormsInitialSize(System.Drawing.Size size)
    {
        _formSize = size;        
    }

    public void StoreControlsInitialSizes()
    {
        ////_formSize = form.Size; //always do the initialization
        var _controls = _get_all_controls(form);//call the enumerator
        foreach (Control control in _controls) //Loop through the controls
        {
            _arr_control_storage.Add(new _addInfo(control,
                control.Bounds,
                control.Font.Size)); //saves control/ bounds/dimension /font size 

            //If you have datagridview
            if (control.GetType() == typeof(DataGridView))
                _dgv_Column_Adjust(((DataGridView)control), showRowHeader);
        }
    }

    public void _resize() //Set the resize
    {
        double _form_ratio_width = (double)form.ClientSize.Width / (double)_formSize.Width; //ratio could be greater or less than 1
        double _form_ratio_height = (double)form.ClientSize.Height / (double)_formSize.Height; // this one too

        foreach (var _item in _arr_control_storage)
        {

            _fontsize = (float)(((_item._fontsize_ * _form_ratio_width) / 2) +
                ((_item._fontsize_ * _form_ratio_height) / 2));
            //Font AutoSize Gradual Change

            if (_fontsize > 0 && _fontsize <= System.Single.MaxValue)
                _item._control.Font = new System.Drawing.Font(form.Font.FontFamily, _fontsize);


            // do some math calc
            //set position
            System.Drawing.Point _controlposition = new System.Drawing.Point((int)
               (_item._bound.X * _form_ratio_width), (int)(_item._bound.Y * _form_ratio_height));//use for location

            //set bounds
            System.Drawing.Size _controlSize = new System.Drawing.Size((int)(_item._bound.Width * _form_ratio_width),
               (int)(_item._bound.Height * _form_ratio_height)); //use for sizing

            _item._control.Bounds = new System.Drawing.Rectangle(_controlposition, _controlSize);

            //Assuming you have a datagridview inside a form()
            //if you want to show the row header, replace the false statement of 
            //showRowHeader on top/public declaration to true;

            if (_item._control.GetType() == typeof(DataGridView))
                _dgv_Column_Adjust(((DataGridView)_item._control), showRowHeader);
        }
    }

    private void _dgv_Column_Adjust(DataGridView dgv, bool _showRowHeader) //if you have Datagridview 
    //and want to resize the column base on its dimension.
    {
        int intRowHeader = 0;
        const int Hscrollbarwidth = 5;
        if (_showRowHeader)
            intRowHeader = dgv.RowHeadersWidth;
        else
            dgv.RowHeadersVisible = false;

        for (int i = 0; i < dgv.ColumnCount; i++)
        {
            if (dgv.Dock == DockStyle.Fill) //in case the datagridview is docked
                dgv.Columns[i].Width = ((dgv.Width - intRowHeader) / dgv.ColumnCount);
            else
                dgv.Columns[i].Width = ((dgv.Width - intRowHeader - Hscrollbarwidth) / dgv.ColumnCount);
        }
    }


    public class _addInfo
    {
        public Control _control = new Control();
        public System.Drawing.Rectangle _bound = new System.Drawing.Rectangle();
        public float _fontsize_ = 0;

        public _addInfo(Control control,
            System.Drawing.Rectangle bound,
            float _thisFontSize)
        {
            _control = control;
            _bound = bound;
            _fontsize_ = _thisFontSize;
        }
    }

    //put get_all_control method here
    private static IEnumerable<Control> _get_all_controls(Control c)
    {
        return c.Controls.Cast<Control>().SelectMany(item =>
            _get_all_controls(item)).Concat(c.Controls.Cast<Control>()).Where(control =>
            control.Name != string.Empty);
    }


}
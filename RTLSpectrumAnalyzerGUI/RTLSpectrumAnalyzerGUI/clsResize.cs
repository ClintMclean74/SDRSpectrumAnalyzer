using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;

public class clsResize
{
    //Public Declaration//
    private ArrayList _arr_control_storage = new ArrayList();
    private bool showRowHeader =false;
 
    public clsResize(Form _form_)
    {
        form = _form_; //the calling form
        _formSize = _form_.ClientSize; //Save initial form size
        _fontsize = _form_.Font.Size; //Font size
    }

    private float _fontsize
    {
        get;    set;
    }

    private System.Drawing.SizeF _formSize
    {
        get;  set;
    }

    private Form form
    {
        get;  set;
    }

    public void _resize()
    {
        double _form_ratio_width = (double)form.ClientSize.Width /  (double)_formSize.Width; //ratio is always greater than 1
        double _form_ratio_height = (double) form.ClientSize.Height /  (double)_formSize.Height; // this one too
        
        var _controls = _get_all_controls(form);

        foreach (Control control in _controls)
        {
            // do some math calc
            System.Drawing.Size _controlSize = new System.Drawing.Size((int)(((System.Drawing.Rectangle)(_arr_control_storage[(int)control.Tag])).Width * _form_ratio_width), (int)(((System.Drawing.Rectangle)(_arr_control_storage[(int)control.Tag])).Height * _form_ratio_height));
            System.Drawing.Point _controlposition = new System.Drawing.Point((int)(((System.Drawing.Rectangle)(_arr_control_storage[(int)control.Tag])).X * _form_ratio_width), (int)(((System.Drawing.Rectangle)(_arr_control_storage[(int)control.Tag])).Y * _form_ratio_height));

            //set bounds
            control.Bounds = new System.Drawing.Rectangle(_controlposition ,_controlSize); //Put together

            //Assuming you have a datagridview inside a form()
            //if you want to show the row header, replace the true statement of showRowHeader on top/public declaration to false;
            if (control.GetType() == typeof(DataGridView))
                _dgv_Column_Adjust(((DataGridView)control), showRowHeader);


            //Font AutoSize
            control.Font = new System.Drawing.Font(form.Font.FontFamily,
             (float)(((Convert.ToDouble(_fontsize) * _form_ratio_width) / 2) +
              ((Convert.ToDouble(_fontsize) * _form_ratio_height) / 2)));
          
        }
    }

    
    public void _get_initial_size() //get initial size//
    {
      
        var _controls = _get_all_controls(form);
        foreach (Control control in _controls)
        {          
            _arr_control_storage.Add(control.Bounds);
            control.Tag = _arr_control_storage.Count - 1;// Here i use tag since i am not using it on other purpose
            //You can use your own int counter value, but i suggest to start it to 0

            //If you have datagridview
            if (control.GetType() == typeof(DataGridView))
                _dgv_Column_Adjust(((DataGridView)control), showRowHeader);

        }
    }

    private void _dgv_Column_Adjust(DataGridView dgv, bool _showRowHeader)
    {
        int intRowHeader = 0;
        const int Hscrollbarwidth = 5;
        if (_showRowHeader)
            intRowHeader = dgv.RowHeadersWidth;
        else
            dgv.RowHeadersVisible = false;

        for (int i = 0; i < dgv.ColumnCount; i++)
        {
            if (dgv.Dock == DockStyle.Fill)
                dgv.Columns[i].Width = ((dgv.Width - intRowHeader) / dgv.ColumnCount);
            else
                dgv.Columns[i].Width = ((dgv.Width - intRowHeader - Hscrollbarwidth) / dgv.ColumnCount);
        }

    }

    private static IEnumerable<Control> _get_all_controls(Control c)
    {
        return c.Controls.Cast<Control>().SelectMany(item =>
            _get_all_controls(item)).Concat(c.Controls.Cast<Control>()).Where(control => 
            control.Name != string.Empty);
    }
}

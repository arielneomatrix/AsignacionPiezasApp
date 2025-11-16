using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AsignacionPiezasApp.Services;

namespace AsignacionPiezasApp.Forms
{
    public class EstatusForm : Form
    {
        private readonly TextBox _txtNombre = new() { PlaceholderText = "Nombre del estatus (p.ej. En progreso)" };
        private readonly Button _btnGuardar = new() { Text = "Guardar" };
        private readonly Button _btnNuevo = new() { Text = "Nuevo" };
        private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

        public EstatusForm()
        {
            Text = "Estatus de piezas";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 420);

            var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(10) };
            top.ColumnCount = 3;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.Controls.Add(_txtNombre, 0, 0);
            top.SetColumnSpan(_txtNombre, 3);
            top.Controls.Add(_btnGuardar, 1, 1);
            top.Controls.Add(_btnNuevo, 2, 1);

            Controls.Add(_grid);
            Controls.Add(top);

            _btnGuardar.Click += (_, __) => { DataService.Instance.AddEstatus(_txtNombre.Text); _txtNombre.Clear(); LoadGrid(); MessageBox.Show("Estatus guardado."); };
            _btnNuevo.Click += (_, __) => _txtNombre.Clear();

            LoadGrid();
            DataService.Instance.EstatusChanged += LoadGrid;
            FormClosed += (_, __) => DataService.Instance.EstatusChanged -= LoadGrid;
        }

        private void LoadGrid()
        {
            var data = DataService.Instance.GetEstatus().Select(e => new { e.Id, e.Nombre }).ToList();
            _grid.DataSource = data;
            if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
        }
    }
}
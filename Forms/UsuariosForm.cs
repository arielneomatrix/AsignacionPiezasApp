using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AsignacionPiezasApp.Services;

namespace AsignacionPiezasApp.Forms
{
    public class UsuariosForm : Form
    {
        private readonly TextBox _txtNombre = new() { PlaceholderText = "Nombre real (p. ej. Juan Pérez)" };
        private readonly TextBox _txtUser = new() { PlaceholderText = "Nombre de usuario (p. ej. jperez)" };
        private readonly Button _btnGuardar = new() { Text = "Guardar" };
        private readonly Button _btnNuevo = new() { Text = "Nuevo" };
        private readonly Button _btnEliminar = new() { Text = "Eliminar" };
        private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

        private Guid? _selectedId = null;

        public UsuariosForm()
        {
            Text = "Usuarios";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(680, 460);

            var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(10) };
            top.ColumnCount = 4;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            top.Controls.Add(_txtNombre, 0, 0);
            top.Controls.Add(_txtUser, 1, 0);
            top.SetColumnSpan(_txtNombre, 1);
            top.SetColumnSpan(_txtUser, 1);
            top.Controls.Add(_btnGuardar, 2, 1);
            top.Controls.Add(_btnNuevo, 3, 1);

            var bottomButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
            bottomButtons.Controls.Add(_btnEliminar);

            Controls.Add(_grid);
            Controls.Add(bottomButtons);
            Controls.Add(top);

            LoadGrid();

            _btnNuevo.Click += (_, __) => ClearForm();
            _btnGuardar.Click += (_, __) => Save();
            _btnEliminar.Click += (_, __) => Delete();

            _grid.SelectionChanged += (_, __) => LoadFromGridSelection();
            _grid.CellDoubleClick += (_, __) => LoadFromGridSelection();

            DataService.Instance.UsuariosChanged += LoadGrid;
            FormClosed += (_, __) => DataService.Instance.UsuariosChanged -= LoadGrid;
        }

        private void LoadGrid()
        {
            var data = DataService.Instance.GetUsuarios()
                .Select(u => new { u.Id, u.Nombre, Usuario = u.NombreUsuario })
                .ToList();
            _grid.DataSource = data;
            if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
        }

        private void LoadFromGridSelection()
        {
            if (_grid.CurrentRow?.DataBoundItem is null) return;
            var row = _grid.CurrentRow;
            _selectedId = Guid.Parse(row.Cells["Id"].Value!.ToString()!);
            _txtNombre.Text = row.Cells["Nombre"].Value?.ToString() ?? "";
            _txtUser.Text = row.Cells["Usuario"].Value?.ToString() ?? "";
        }

        private void ClearForm()
        {
            _selectedId = null;
            _txtNombre.Clear();
            _txtUser.Clear();
            _grid.ClearSelection();
            _txtNombre.Focus();
        }

        private void Save()
        {
            var nombre = _txtNombre.Text.Trim();
            var user = _txtUser.Text.Trim();

            if (_selectedId is null)
            {
                if (!DataService.Instance.AddUsuario(nombre, user, out var error))
                { MessageBox.Show(error, "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                MessageBox.Show("Usuario creado.");
            }
            else
            {
                if (!DataService.Instance.UpdateUsuario(_selectedId.Value, nombre, user, out var error))
                { MessageBox.Show(error, "No se pudo actualizar", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                MessageBox.Show("Usuario actualizado.");
            }

            ClearForm();
            LoadGrid();
        }

        private void Delete()
        {
            if (_selectedId is null) { MessageBox.Show("Selecciona un usuario en la lista."); return; }

            var confirm = MessageBox.Show("¿Eliminar el usuario seleccionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            if (!DataService.Instance.DeleteUsuario(_selectedId.Value, out var error))
            {
                MessageBox.Show(error, "No se pudo eliminar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Usuario eliminado.");
            ClearForm();
            LoadGrid();
        }
    }
}

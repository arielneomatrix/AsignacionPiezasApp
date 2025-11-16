using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AsignacionPiezasApp.Models;
using AsignacionPiezasApp.Services;

namespace AsignacionPiezasApp.Forms
{
    public class PiezasForm : Form
    {
        private readonly TextBox _txtCodigo = new() { PlaceholderText = "Código de pieza" };
        private readonly TextBox _txtDescripcion = new()
        {
            PlaceholderText = "Descripción y comentarios",
            Multiline = true,
            AcceptsReturn = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            Width = 260,   // ancho
            Height = 60   // alto = ancho -> “cuadro”
        };
        private readonly ComboBox _cmbUsuario = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _cmbEstatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly PictureBox _pic = new() { SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, Height = 180 };
        private readonly Button _btnCargarFoto = new() { Text = "Cargar foto..." };

        private readonly Button _btnGuardar = new() { Text = "Guardar" };
        private readonly Button _btnNuevo = new() { Text = "Nuevo registro" };
        private readonly Button _btnBuscar = new() { Text = "Buscar Código" };

        private readonly Label _lblFecha = new() { Text = "", AutoSize = true };

        private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly TextBox _txtFiltroCodigo = new() { PlaceholderText = "Filtrar por código..." };
        private readonly ComboBox _cmbFiltroUsuario = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _cmbFiltroEstatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Button _btnAplicarFiltros = new() { Text = "Aplicar filtros" };
        private readonly Button _btnLimpiarFiltros = new() { Text = "Limpiar" };

        private string? _fotoPathActual;

        public PiezasForm()
        {
            Text = "Asignación de piezas";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1080, 910);
            // ... (aquí ya existen _grid y los demás controles, porque se crean como fields)

            // <<< Pega esta línea >>>
            ConfigurarGrid(_grid);

            // arma el layout, agrega _grid al panel derecho, etc.
            // right.Controls.Add(_grid);
            // ...
            LoadCombos();
            LoadGrid();

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

            var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 9 };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            left.Controls.Add(MakeLabeled("Código:", _txtCodigo));
            left.Controls.Add(MakeLabeled("Usuario asignado:", _cmbUsuario));
            left.Controls.Add(MakeLabeled("Estatus:", _cmbEstatus));

            var fotoPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill };
            fotoPanel.Controls.Add(_btnCargarFoto);
            left.Controls.Add(MakeLabeled("Cargar foto:",fotoPanel));
            left.Controls.Add(_lblFecha);
            left.Controls.Add(MakeLabeled("Descripción y comentarios:", _txtDescripcion));

            var acciones = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill };
            acciones.Controls.AddRange(new Control[] { _btnGuardar, _btnNuevo, _btnBuscar });
            left.Controls.Add(acciones);

            var rightImagePanel = new Panel { Dock = DockStyle.Fill, Height = 450 };
            _pic.Dock = DockStyle.Fill;
            rightImagePanel.Controls.Add(_pic);
            left.Controls.Add(rightImagePanel);

            var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, AutoSize = true };
            filtros.Controls.Add(new Label { Text = "Código:" });
            filtros.Controls.Add(_txtFiltroCodigo);
            filtros.Controls.Add(new Label { Text = "Usuario:" });
            filtros.Controls.Add(_cmbFiltroUsuario);
            filtros.Controls.Add(new Label { Text = "Estatus:" });
            filtros.Controls.Add(_cmbFiltroEstatus);
            filtros.Controls.Add(_btnAplicarFiltros);
            filtros.Controls.Add(_btnLimpiarFiltros);

            var right = new Panel { Dock = DockStyle.Fill };
            right.Controls.Add(_grid);
            right.Controls.Add(filtros);

            main.Controls.Add(left, 0, 0);
            main.Controls.Add(right, 1, 0);
            Controls.Add(main);

            LoadCombos();
            LoadGrid();

            DataService.Instance.UsuariosChanged += () => { LoadCombos(); LoadGrid(); };
            DataService.Instance.EstatusChanged += () => { LoadCombos(); LoadGrid(); };
            DataService.Instance.PiezasChanged += LoadGrid;

            _btnNuevo.Click += (_, __) => ClearForm();
            _btnCargarFoto.Click += (_, __) => LoadPhoto();
            _btnGuardar.Click += (_, __) => SaveRecord();
            _btnBuscar.Click += (_, __) => BuscarPorCodigo();

            _btnAplicarFiltros.Click += (_, __) => LoadGrid();
            _btnLimpiarFiltros.Click += (_, __) => { _txtFiltroCodigo.Clear(); _cmbFiltroUsuario.SelectedIndex = 0; _cmbFiltroEstatus.SelectedIndex = 0; LoadGrid(); };
            // Enter en el campo "Código" del filtro = Aplicar
            _txtFiltroCodigo.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // evita el "beep"
                    LoadGrid();                // mismo efecto que el botón Aplicar
                }
            };

            // (Opcional) Solo mientras el foco esté en el filtro, Enter también dispara el botón Aplicar
            _txtFiltroCodigo.Enter += (s, e) => this.AcceptButton = _btnAplicarFiltros;
            _txtFiltroCodigo.Leave += (s, e) => this.AcceptButton = null;

            // (Opcional) Si también quieres que Enter funcione cuando el foco esté en los combos de filtro:
            _cmbFiltroUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadGrid(); } };
            _cmbFiltroEstatus.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadGrid(); } };

            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) LoadFromGrid(e.RowIndex); };
        }

        private Control MakeLabeled(string label, Control input)
        {
            var panel = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Top, AutoSize = true };
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 6, 0, 2) });
            input.Width = 340;
            panel.Controls.Add(input);
            return panel;
        }

        private void LoadCombos()
        {
            var usrs = DataService.Instance.GetUsuarios().ToList();
            var sts = DataService.Instance.GetEstatus().ToList();

            _cmbUsuario.DataSource = null; _cmbUsuario.DataSource = usrs; _cmbUsuario.DisplayMember = "Nombre"; _cmbUsuario.ValueMember = "Id";
            _cmbEstatus.DataSource = null; _cmbEstatus.DataSource = sts; _cmbEstatus.DisplayMember = "Nombre"; _cmbEstatus.ValueMember = "Id";

            var usrsFilter = new[] { new { Id = (Guid?)null, Nombre = "Todos" } }.Concat(usrs.Select(u => new { Id = (Guid?)u.Id, u.Nombre })).ToList();
            var stsFilter = new[] { new { Id = (Guid?)null, Nombre = "Todos" } }.Concat(sts.Select(e => new { Id = (Guid?)e.Id, e.Nombre })).ToList();

            _cmbFiltroUsuario.DataSource = usrsFilter; _cmbFiltroUsuario.DisplayMember = "Nombre"; _cmbFiltroUsuario.ValueMember = "Id";
            _cmbFiltroEstatus.DataSource = stsFilter; _cmbFiltroEstatus.DisplayMember = "Nombre"; _cmbFiltroEstatus.ValueMember = "Id";
        }

        private void LoadGrid()
        {
            Guid? userId = _cmbFiltroUsuario.SelectedValue as Guid?;
            Guid? statusId = _cmbFiltroEstatus.SelectedValue as Guid?;

            var piezas = DataService.Instance.GetPiezas(_txtFiltroCodigo.Text.Trim(), userId, statusId)
                .Select(p => new
                {
                    p.Codigo,
                    p.Descripcion,
                    Usuario = DataService.Instance.GetUsuarioNombre(p.UsuarioId),
                    Estatus = DataService.Instance.GetEstatusNombre(p.EstatusId),
                    p.FotoPath,
                    Fecha = p.FechaRegistro
                }).ToList();

            _grid.DataSource = piezas;
            if (_grid.Columns.Contains("FotoPath")) _grid.Columns["FotoPath"].Visible = false;
        }

        private void ClearForm()
        {
            _txtCodigo.Text = "";
            _txtDescripcion.Text = "";
            _cmbUsuario.SelectedIndex = _cmbUsuario.Items.Count > 0 ? 0 : -1;
            _cmbEstatus.SelectedIndex = _cmbEstatus.Items.Count > 0 ? 0 : -1;
            _pic.ImageLocation = null; _fotoPathActual = null;
            _lblFecha.Text = "Fecha/hora: (auto)";
            _txtCodigo.Focus();
        }

        private void LoadPhoto()
        {
            using var ofd = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp", Title = "Selecciona una foto" };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(ofd.FileName)}";
                var dest = Path.Combine(Database.FotosDir, fileName);
                File.Copy(ofd.FileName, dest, true);
                _fotoPathActual = dest; _pic.ImageLocation = dest;
            }
        }

        private void SaveRecord()
        {
            if (string.IsNullOrWhiteSpace(_txtCodigo.Text)) { MessageBox.Show("El código de pieza es obligatorio."); return; }
            var pieza = new AsignacionPieza
            {
                Codigo = _txtCodigo.Text.Trim(),
                Descripcion = _txtDescripcion.Text.Trim(),
                UsuarioId = _cmbUsuario.SelectedItem is null ? null : (Guid?)((dynamic)_cmbUsuario.SelectedItem).Id,
                EstatusId = _cmbEstatus.SelectedItem is null ? null : (Guid?)((dynamic)_cmbEstatus.SelectedItem).Id,
                FotoPath = _fotoPathActual
            };
            var existing = DataService.Instance.FindByCodigo(pieza.Codigo);
            DataService.Instance.AddOrUpdatePieza(pieza);
            var fecha = (existing?.FechaRegistro ?? DateTime.Now);
            _lblFecha.Text = $"Fecha/hora: {fecha:G}";
            LoadGrid();
            MessageBox.Show("Registro guardado.");
        }

        private void BuscarPorCodigo()
        {
            var codigo = _txtCodigo.Text.Trim();
            if (string.IsNullOrWhiteSpace(codigo)) { MessageBox.Show("Escribe un código para buscar."); return; }
            var p = DataService.Instance.FindByCodigo(codigo);
            if (p is null) { MessageBox.Show("No se encontró la pieza."); return; }
            LoadPieceIntoForm(p);
        }

        private void LoadFromGrid(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;
            var codigo = _grid.Rows[rowIndex].Cells["Codigo"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(codigo)) return;
            var p = DataService.Instance.FindByCodigo(codigo);
            if (p is null) return;
            LoadPieceIntoForm(p);
        }

        private void LoadPieceIntoForm(AsignacionPieza p)
        {
            _txtCodigo.Text = p.Codigo;
            _txtDescripcion.Text = p.Descripcion;
            _fotoPathActual = p.FotoPath; _pic.ImageLocation = p.FotoPath;
            _lblFecha.Text = $"Fecha/hora: {p.FechaRegistro:G}";

            if (p.UsuarioId is not null)
            {
                for (int i = 0; i < _cmbUsuario.Items.Count; i++)
                {
                    var it = (dynamic)_cmbUsuario.Items[i];
                    if ((Guid)it.Id == p.UsuarioId) { _cmbUsuario.SelectedIndex = i; break; }
                }
            }
            if (p.EstatusId is not null)
            {
                for (int i = 0; i < _cmbEstatus.Items.Count; i++)
                {
                    var it = (dynamic)_cmbEstatus.Items[i];
                    if ((Guid)it.Id == p.EstatusId) { _cmbEstatus.SelectedIndex = i; break; }
                }
            }
        }

        // Ajusta tipografías y alturas del DataGridView
        private void ConfigurarGrid(DataGridView grid)
        {
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 12f);                     // tamaño celdas
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12f, FontStyle.Bold); // encabezados
            grid.EnableHeadersVisualStyles = false;                                     // respeta nuestro estilo

            // Alturas para que no se corte el texto
            grid.RowTemplate.Height = 30;        // alto de fila (sube o baja a tu gusto)
            grid.ColumnHeadersHeight = 34;       // alto de encabezado

            // Opcionales:
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;  // o AllCells si quieres que las filas crezcan
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False; // False = una línea, True = multilínea
        }

    }
}
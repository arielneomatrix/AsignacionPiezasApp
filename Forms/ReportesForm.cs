using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using AsignacionPiezasApp.Services;

namespace AsignacionPiezasApp.Forms
{
    // Ventana para crear informes (sin diseñador)
    public class ReportesForm : Form
    {
        // Filtros
        private readonly TextBox _txtCodigo = new() { PlaceholderText = "Código contiene..." };
        private readonly TextBox _txtDescripcion = new() { PlaceholderText = "Descripción contiene..." };
        private readonly ComboBox _cmbUsuario = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _cmbEstatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DateTimePicker _dtDesde = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
        private readonly DateTimePicker _dtHasta = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };

        // Orden
        private readonly ComboBox _cmbOrden = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _cmbSentido = new() { DropDownStyle = ComboBoxStyle.DropDownList };

        // Acciones
        private readonly Button _btnAplicar = new() { Text = "Aplicar" };
        private readonly Button _btnExportar = new() { Text = "Exportar PDF" };

        // Vista previa
        private readonly DataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        public ReportesForm()
        {
            Text = "Crear informes";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1024, 640);

            var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 110, ColumnCount = 8, Padding = new Padding(8) };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5));

            top.Controls.Add(MakeLabeled("Código:", _txtCodigo), 0, 0);
            top.Controls.Add(MakeLabeled("Descripción:", _txtDescripcion), 1, 0);
            top.Controls.Add(MakeLabeled("Usuario:", _cmbUsuario), 2, 0);
            top.Controls.Add(MakeLabeled("Estatus:", _cmbEstatus), 3, 0);
            top.Controls.Add(MakeLabeled("Desde:", _dtDesde), 4, 0);
            top.Controls.Add(MakeLabeled("Hasta:", _dtHasta), 5, 0);
            top.Controls.Add(MakeLabeled("Orden:", _cmbOrden), 6, 0);
            top.Controls.Add(MakeLabeled("Sentido:", _cmbSentido), 7, 0);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8) };
            actions.Controls.Add(_btnAplicar);
            actions.Controls.Add(_btnExportar);

            Controls.Add(_grid);
            Controls.Add(actions);
            Controls.Add(top);

            LoadCombos();

            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _grid.EnableHeadersVisualStyles = false;
            _grid.RowTemplate.Height = 28;
            _grid.ColumnHeadersHeight = 32;

            _btnAplicar.Click += (_, __) => LoadPreview();
            _btnExportar.Click += (_, __) => ExportPdf();

            _txtCodigo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadPreview(); } };
            _txtDescripcion.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadPreview(); } };

            LoadPreview();
        }

        private Control MakeLabeled(string label, Control input)
        {
            var p = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, AutoSize = true };
            p.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 0, 0, 2) });
            p.Controls.Add(input);
            return p;
        }

        private static string Sanitize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Todos";
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s.Trim();
        }

        private void LoadCombos()
        {
            var usuarios = DataService.Instance.GetUsuarios()
                .Select(u => new { u.Id, Nombre = string.IsNullOrWhiteSpace(u.NombreUsuario) ? u.Nombre : $"{u.Nombre} ({u.NombreUsuario})" }).ToList();
            usuarios.Insert(0, new { Id = Guid.Empty, Nombre = "Todos" });
            _cmbUsuario.DataSource = usuarios; _cmbUsuario.DisplayMember = "Nombre"; _cmbUsuario.ValueMember = "Id";

            var estatus = DataService.Instance.GetEstatus().Select(e => new { e.Id, e.Nombre }).ToList();
            estatus.Insert(0, new { Id = Guid.Empty, Nombre = "Todos" });
            _cmbEstatus.DataSource = estatus; _cmbEstatus.DisplayMember = "Nombre"; _cmbEstatus.ValueMember = "Id";

            _cmbOrden.Items.AddRange(new object[] { "Código", "Descripción", "Usuario", "Estatus", "Fecha" });
            _cmbOrden.SelectedIndex = 4;
            _cmbSentido.Items.AddRange(new object[] { "Asc", "Desc" });
            _cmbSentido.SelectedIndex = 1;
        }

        private (ReportOrderBy orderBy, bool desc) ReadOrder()
        {
            var order = _cmbOrden.SelectedItem?.ToString() ?? "Fecha";
            var desc = (_cmbSentido.SelectedItem?.ToString() ?? "Desc").Equals("Desc", StringComparison.OrdinalIgnoreCase);
            var ob = order switch
            {
                "Código" => ReportOrderBy.Codigo,
                "Descripción" => ReportOrderBy.Descripcion,
                "Usuario" => ReportOrderBy.Usuario,
                "Estatus" => ReportOrderBy.Estatus,
                _ => ReportOrderBy.Fecha
            };
            return (ob, desc);
        }

        private void LoadPreview()
        {
            Guid? usuarioId = (_cmbUsuario.SelectedValue is Guid g && g != Guid.Empty) ? g : null;
            Guid? estatusId = (_cmbEstatus.SelectedValue is Guid g2 && g2 != Guid.Empty) ? g2 : null;
            DateTime? desde = _dtDesde.Checked ? _dtDesde.Value.Date : null;
            DateTime? hasta = _dtHasta.Checked ? _dtHasta.Value.Date.AddDays(1).AddSeconds(-1) : null;
            var (orderBy, desc) = ReadOrder();

            var data = DataService.Instance.GetPiezasAdvanced(
                codigoLike: _txtCodigo.Text.Trim(),
                usuarioId: usuarioId,
                estatusId: estatusId,
                fechaDesde: desde,
                fechaHasta: hasta,
                descripcionLike: _txtDescripcion.Text.Trim(),
                orderBy: orderBy,
                orderDesc: desc
            ).Select(p => new
            {
                p.Codigo,
                p.Descripcion,
                Usuario = DataService.Instance.GetUsuarioNombre(p.UsuarioId) ?? "",
                Estatus = DataService.Instance.GetEstatusNombre(p.EstatusId) ?? "",
                Fecha = p.FechaRegistro
            }).ToList();

            _grid.DataSource = data;
        }

        private string BuildReportFolder()
        {
            string usuario = (_cmbUsuario.SelectedIndex <= 0) ? "Todos" : Sanitize(_cmbUsuario.Text);
            string estatus = (_cmbEstatus.SelectedIndex <= 0) ? "Todos" : Sanitize(_cmbEstatus.Text);
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string folder = Path.Combine(Database.ReportsRoot, usuario, estatus, fecha);
            Directory.CreateDirectory(folder);
            return folder;
        }

        private string BuildFiltroResumen()
        {
            string u = (_cmbUsuario.SelectedIndex <= 0) ? "Todos" : _cmbUsuario.Text;
            string e = (_cmbEstatus.SelectedIndex <= 0) ? "Todos" : _cmbEstatus.Text;
            string c = string.IsNullOrWhiteSpace(_txtCodigo.Text) ? "-" : _txtCodigo.Text.Trim();
            string d = string.IsNullOrWhiteSpace(_txtDescripcion.Text) ? "-" : _txtDescripcion.Text.Trim();
            string fd = _dtDesde.Checked ? _dtDesde.Value.ToString("yyyy-MM-dd") : "-";
            string fh = _dtHasta.Checked ? _dtHasta.Value.ToString("yyyy-MM-dd") : "-";
            var (ob, desc) = ReadOrder();
            return $"Usuario: {u} | Estatus: {e} | Código: {c} | Descripción: {d} | Desde: {fd} | Hasta: {fh} | Orden: {ob} {(desc ? "Desc" : "Asc")}";
        }

        private void ExportPdf()
        {
            if (_grid.DataSource is null)
            {
                MessageBox.Show(this, "No hay datos para exportar.", "Informe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var targetFolder = BuildReportFolder();
            string slugUsuario = (_cmbUsuario.SelectedIndex <= 0) ? "todos" : Sanitize(_cmbUsuario.Text).ToLower();
            string slugEstatus = (_cmbEstatus.SelectedIndex <= 0) ? "todos" : Sanitize(_cmbEstatus.Text).ToLower();
            string fileNameSuggested = $"informe_{slugUsuario}_{slugEstatus}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            using var sfd = new SaveFileDialog
            {
                Title = "Guardar informe",
                Filter = "PDF (*.pdf)|*.pdf",
                AddExtension = true,
                InitialDirectory = targetFolder,
                FileName = fileNameSuggested
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            var rows = (_grid.DataSource as System.Collections.IEnumerable)!
                .Cast<dynamic>()
                .Select(x => new ReportPdfService.ReportRow
                {
                    Codigo = x.Codigo,
                    Descripcion = x.Descripcion,
                    Usuario = x.Usuario,
                    Estatus = x.Estatus,
                    Fecha = (x.Fecha is DateTime dt) ? dt.ToString("yyyy-MM-dd HH:mm") : x.Fecha?.ToString()
                })
                .ToList();

            var filtros = BuildFiltroResumen();

            try
            {
                ReportPdfService.GeneratePiezasReportPdf(
                    sfd.FileName,
                    rows,
                    new ReportPdfService.ReportInfo { Titulo = "Informe de piezas", FiltrosAplicados = filtros }
                );

                MessageBox.Show(this, $"Informe generado:\n{sfd.FileName}", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo generar el PDF.\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}


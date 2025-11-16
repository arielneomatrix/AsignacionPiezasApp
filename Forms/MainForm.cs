using System.Drawing;
using System.Windows.Forms;

namespace AsignacionPiezasApp.Forms
{
    public class MainForm : Form
    {
        public MainForm()
        {
            Text = "Sistema de Asignación de Piezas";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(560, 280);

            var btnPiezas = new Button { Text = "Asignación de piezas", Width = 220, Height = 52 };
            var btnUsuarios = new Button { Text = "Usuarios", Width = 220, Height = 52 };
            var btnEstatus = new Button { Text = "Estatus de piezas", Width = 220, Height = 52 };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(20) };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            layout.Controls.Add(btnPiezas, 0, 0);
            layout.Controls.Add(btnUsuarios, 0, 1);
            layout.Controls.Add(btnEstatus, 0, 2);
            Controls.Add(layout);

            btnPiezas.Click += (_, __) => new PiezasForm().ShowDialog(this);
            btnUsuarios.Click += (_, __) => new UsuariosForm().ShowDialog(this);
            btnEstatus.Click +=  (_, __) => new EstatusForm().ShowDialog(this);
        }
    }
}
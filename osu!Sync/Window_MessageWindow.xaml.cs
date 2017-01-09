using System.Windows;
using System.Windows.Documents;

namespace osuSync {

    public partial class Window_MessageWindow {

        public Window_MessageWindow() {
            InitializeComponent();
        }

        public void Bu_Close_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		public void SetMessage(string message, string title = null, string subTitle = null) {
			TB_Title.Text = (title == null ? GlobalVar._e("WindowMessage_message") : title);
            TB_SubTitle.Text = (subTitle == null ? GlobalVar.appName : subTitle);

            Paragraph Paragraph = new Paragraph();
			Paragraph.Inlines.Add(new Run(message));

            RTB_Message.Document.Blocks.Clear();
            RTB_Message.Document.Blocks.Add(Paragraph);
		}
	}
}

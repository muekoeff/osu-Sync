using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace osuSync {

    partial class Window_GenericMsgBox : Window {

		public class MsgBoxButtonHolder {
			public ButtonAction Action { get; set; }
			public string Label { get; set; }
			public int ResultId { get; set; }

			public MsgBoxButtonHolder(string Label, int ResultId, ButtonAction Action = ButtonAction.None) {
				this.Label = Label;
				this.ResultId = ResultId;
				this.Action = Action;
			}

			public MsgBoxButtonHolder(MsgBoxResult CoreButton) {
				switch(CoreButton) {
					case MsgBoxResult.Ok:
						Label = GlobalVar._e("Global_buttons_ok");
						ResultId = (int)MsgBoxResult.Ok;
						Action = ButtonAction.Ok;
						break;
					case MsgBoxResult.Cancel:
						Label = GlobalVar._e("Global_buttons_cancel");
						ResultId = (int)MsgBoxResult.Cancel;
						Action = ButtonAction.Cancel;
						break;
					case MsgBoxResult.Yes:
						Label = GlobalVar._e("Global_buttons_yes");
						ResultId = (int)MsgBoxResult.Yes;
						Action = ButtonAction.Ok;
						break;
					case MsgBoxResult.YesAll:
						Label = GlobalVar._e("Global_buttons_yesForAll");
						ResultId = (int)MsgBoxResult.YesAll;
						break;
					case MsgBoxResult.No:
						Label = GlobalVar._e("Global_buttons_no");
						ResultId = (int)MsgBoxResult.No;
						Action = ButtonAction.Cancel;
						break;
					case MsgBoxResult.NoAll:
						Label = GlobalVar._e("Global_buttons_noForAll");
						ResultId = (int)MsgBoxResult.NoAll;
						break;
				}

			}
		}

		public enum ButtonAction {
			None = 0,
			Ok = 1,
			Cancel = 2
		}

		public enum MsgBoxResult {
			None = 0,
			Ok = 1,
			Cancel = 2,
			Yes = 6,
			YesAll = 61,
			No = 7,
			NoAll = 71
		}

		public enum SimpleMsgBoxButton{
			Ok,
			OkCancel
		}

		public List<MsgBoxButtonHolder> Buttons {
			set {
				SP_ButtonWrapper.Children.Clear();
				foreach (MsgBoxButtonHolder i in value) {
                    Button thisButton = new Button() {
                        Content = i.Label,
                        Margin = new Thickness(5, 0, 0, 0),
                        Padding = new Thickness(10, 10, 10, 10),
                        Tag = i.ResultId
                    };

                    if(i.Action == ButtonAction.Cancel) {
                        thisButton.IsCancel = true;
					} else if (i.Action == ButtonAction.Ok) {
                        thisButton.IsDefault = true;
					}
                    thisButton.Click += Bu_Click;
					SP_ButtonWrapper.Children.Add(thisButton);
				}
			}
		}

		public string Caption {
			get { return Title; }
			set {
				if(value == null)
					Title = GlobalVar.appName;
				else
					Title = value;
			}
		}

		public Icon MessageBoxIcon {
			set {
				if(value == null) {
					CD_Icon.Width = new GridLength(0);  // Hide Icon column
				} else {
					CD_Icon.Width = new GridLength(55); // Show Icon column
					Im_Icon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(value.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				}
			}
		}

		public string MessageBoxText {
			get { return TB_Text.Text; }
			set { TB_Text.Text = value; }
		}

		public int Result { get; set; }

		public SimpleMsgBoxButton SimpleButton {
			set {
				switch (value) {
					case SimpleMsgBoxButton.Ok:
						Buttons = new List<MsgBoxButtonHolder> { new MsgBoxButtonHolder(GlobalVar._e("Global_buttons_ok"), (int)MessageBoxResult.OK) };
						break;
					case SimpleMsgBoxButton.OkCancel:
						Buttons = new List<MsgBoxButtonHolder> {
							new MsgBoxButtonHolder(GlobalVar._e("Global_buttons_ok"), (int)MessageBoxResult.OK),
							new MsgBoxButtonHolder(GlobalVar._e("Global_buttons_cancel"), (int)MessageBoxResult.Cancel)
						};
						break;
				}
			}
		}

		public Window_GenericMsgBox(string MessageBoxText, SimpleMsgBoxButton SimpleButton = SimpleMsgBoxButton.Ok, string Caption = null, Icon MessageBoxIcon = null) {
			InitializeComponent();
			this.MessageBoxText = MessageBoxText;
            this.SimpleButton = SimpleButton;
            this.Caption = Caption;
            this.MessageBoxIcon = MessageBoxIcon;
		}

		public Window_GenericMsgBox(string MessageBoxText, List<MsgBoxButtonHolder> Buttons, string Caption = null, Icon MessageBoxIcon = null) {
			InitializeComponent();
			this.MessageBoxText = MessageBoxText;
            this.Buttons = Buttons;
            this.Caption = Caption;
            this.MessageBoxIcon = MessageBoxIcon;
		}

		private void Bu_Click(object sender, RoutedEventArgs e) {
			Result = Convert.ToInt32(((Button)sender).Tag);
			Close();
		}
	}
}

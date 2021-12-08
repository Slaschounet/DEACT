using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.View;

namespace TTSDeckEditAndCreationTool.ViewModel
{
    class FeedbackPopupViewModel
    {
        private static FeedbackPopupViewModel _instance;
        public static FeedbackPopupViewModel Instance
        {
            get
            {
                if (_instance == null)
                    return _instance = new FeedbackPopupViewModel();
                else
                    return _instance;
            }
        }

        public static string NotifLabel { get; set; }
        public static string Message { get; set; }

        public void DisplayOkMessage(string message)
        {
            NotifLabel = "Notice";

            Message = message;

            FeedbackPopupView PopupView = new FeedbackPopupView();
            PopupView.DataContext = _instance;
            PopupView.ShowDialog();
        }

        public void DisplaySmileMessage(string message)
        {
            NotifLabel = ": )";

            Message = message;

            FeedbackPopupView PopupView = new FeedbackPopupView();
            PopupView.DataContext = _instance;
            PopupView.ShowDialog();
        }

        public void DisplayErrorMessage(string message)
        {
            NotifLabel = "ERROR";

            Message = message;

            FeedbackPopupView PopupView = new FeedbackPopupView();
            PopupView.DataContext = _instance;
            PopupView.ShowDialog();
        }
    }
}

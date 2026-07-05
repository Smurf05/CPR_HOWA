using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Framework.Chatbot;

namespace HOWA.Admin.ViewModels
{
    public class ChatMessage
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }
        public System.DateTime Timestamp { get; set; } = System.DateTime.Now;
    }

    public class ChatbotViewModel : BaseViewModel
    {
        private readonly ChatbotService _chatbotService;
        private ObservableCollection<ChatMessage> _messages;
        private string _userText;

        public ChatbotViewModel(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
            Messages = new ObservableCollection<ChatMessage>();
            SendCommand = new Microsoft.Maui.Controls.Command(async () => await ExecuteSendAsync());
            Messages.Add(new ChatMessage 
            { 
                Text = "Welcome to HOWA Assistance! Ask me about statistics, registration guides, or check-in status.", 
                IsUser = false 
            });
        }

        public ObservableCollection<ChatMessage> Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        public string UserText
        {
            get => _userText;
            set => SetProperty(ref _userText, value);
        }

        public ICommand SendCommand { get; }

        private async Task ExecuteSendAsync()
        {
            if (string.IsNullOrWhiteSpace(UserText)) return;

            var userMsg = UserText;
            UserText = string.Empty;

            Messages.Add(new ChatMessage { Text = userMsg, IsUser = true });

            IsBusy = true;
            try
            {
                var reply = await _chatbotService.ProcessQueryAsync(userMsg);
                Messages.Add(new ChatMessage { Text = reply, IsUser = false });
            }
            catch (System.Exception ex)
            {
                Messages.Add(new ChatMessage { Text = $"Chatbot error: {ex.Message}", IsUser = false });
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

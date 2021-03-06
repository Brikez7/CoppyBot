using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using MessagingToolkit.QRCode.Codec;
using MessagingToolkit.QRCode.Codec.Data;
using System.Drawing;
using System.Text;
using File = Telegram.Bot.Types.File;

namespace TelegramBotExperiments
{
    public class MyBot
    {
        public static ITelegramBotClient bot;
        public readonly static string[] Active = new string[] { "/Некорректная команда", "/start", "/Mgotext", "/Mgoqrcode", "/gotext", "/goqrcode","/error" };
        private static bool ToText = false, ToQRCode = false;
        private static int IndexActiv = 0;

        static MyBot() => bot = new TelegramBotClient("5302452964:AAER1OTaCStBCRXnkRKyDCd4UdjtVQ88Gas");
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                Message message = update.Message;

                Change(message);

                switch (Active[IndexActiv])
                {
                    case "/Некорректная команда":
                        await ErrorCommand(botClient, message);
                        return;

                    case "/start":
                        await ShowHi(botClient, message);
                        return;

                    case "/Mgotext":
                        await ShowMessageGoText(botClient, message);
                        return;

                    case "/Mgoqrcode":
                        await ShowMessageGoQR(botClient, message);
                        return;

                    case "/gotext":
                        string path = "QRCodeToText.ru";
                        //DeleteFile(path);

                        DownloadQRCode(botClient, message.Photo[^1].FileId, path);

                        DecoderQRCode(botClient, message, path);

                        DeleteFile(path);

                        Console.WriteLine();
                        return;

                    case "/goqrcode":
                        await CoderToQRCode(botClient, message);
                        return;

                    case "/error":
                        await CriticalErrorCommand(botClient, message);
                        return;

                }
            }
        }

        private static void Change(Message message) 
        {
            if (message.Text == null)
            {
                if (ToText)
                {
                    IndexActiv = 4;
                }
                else 
                {
                    IndexActiv = 6;
                }
            }
            else
            {
                if (ToQRCode)
                {
                    IndexActiv = 5;
                }
                else
                {
                    switch (message.Text.ToString())
                    {
                        case "/goqrcode":
                            IndexActiv = 3;
                            break;
                        case "/start":
                            IndexActiv = 1;
                            break;
                        case "/gotext":
                            IndexActiv = 2;
                            break;
                        default:
                            IndexActiv = 0;
                            break;
                    }
                }
            }
        }

        private static async Task CriticalErrorCommand(ITelegramBotClient botClient, Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat, "Вы ввели что то очень неприятное");
            return;
        }
        private static async Task ErrorCommand(ITelegramBotClient botClient, Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat, "Вы ввели некорректную команду");
            return;
        }
        private static async Task ShowHi(ITelegramBotClient botClient, Message message)
        {
            await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать на борт, добрый путник!");
            return;
        }
        private static async Task ShowMessageGoQR(ITelegramBotClient botClient, Message message)
        {
            ToQRCode = true;
            ToText = false;
            await botClient.SendTextMessageAsync(message.Chat, "Введите текст.");
            return;
        }
        private static async Task ShowMessageGoText(ITelegramBotClient botClient, Message message)
        {
            ToQRCode = false;
            ToText = true;
            await botClient.SendTextMessageAsync(message.Chat, "Загрузите QRCode.");
            return;
        }
        private static async Task CoderToQRCode(ITelegramBotClient botClient, Message message,string path = "TextToQRCode.ru")
        {
            ToQRCode = ToText = false;

            string mess = message.Text.ToString();

            QRCodeEncoder Enkoder = new QRCodeEncoder();
            Bitmap QRCode =  Enkoder.Encode(mess, Encoding.UTF8);

            QRCode.Save(path);

            using (var Stream = System.IO.File.OpenRead(path))
            {
                await botClient.SendPhotoAsync(message.Chat, Stream);
                Stream.Close();
            }
            return;
        }
        static async void DownloadQRCode(ITelegramBotClient botClient, string fileId, string path)
        {
            try
            {
                File file = await botClient.GetFileAsync(fileId);

                FileStream saveImageStream = new FileStream(path, FileMode.CreateNew);

                await botClient.DownloadFileAsync(file.FilePath, saveImageStream);

                saveImageStream.Close();
                return;
            }
            catch (Exception Error)
            {
                Console.WriteLine("Error downloading: " + Error.Message);
                return;
            }
        }
        private static void DeleteFile(string path)
        {
            FileInfo fileInfo = new(path);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
                Console.WriteLine("Файл что УДАЛЕН");
            }
            else 
            {
                Console.WriteLine("ФАЙЛ  ОТКРЫТ ");
            }
            return;
        }
        private static async Task DecoderQRCode(ITelegramBotClient botClient, Message message,string path)
        {
            ToText = ToQRCode = false;

            Bitmap QRCode = new Bitmap(path);

            QRCodeDecoder decoder = new QRCodeDecoder();
            string Mess = decoder.decode(new QRCodeBitmapImage(QRCode));

            QRCode.Dispose();

            await botClient.SendTextMessageAsync(message.Chat, $"Ваш текст: {Mess}");

            return;
        }
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
        static void Main(string[] args)
        {
            BotStart();
            Console.ReadLine();
        }
        private static void BotStart()
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken cancellationToken = cts.Token;
            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
        }
    }
}
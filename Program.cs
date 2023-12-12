using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class CurrencyServer
{
    private static int countClient = 0;
    private static int maxClients;
    private static int maxRequest;
    private static Dictionary<string, string> userList = new Dictionary<string, string>{};
    private static int newClient;
    static async Task Main(string[] args)
    {
        Console.WriteLine("Введiть максимальну кiлькiсть клiєнтiв:");
        if (int.TryParse(Console.ReadLine(), out int maxClientsInput))
        { maxClients = maxClientsInput;}
        else
        {
            Console.WriteLine("Невiрний формат вводу для максимальної кiлькостi клiєнтiв.\nВстановлено кiлькiсть 2 (за замовченням).");
            maxClients = 2;
        }

        Console.WriteLine("Введiть максимальну кiлькiсть запитiв:");
        if (int.TryParse(Console.ReadLine(), out int maxRequestInput))
        { maxRequest = maxRequestInput;}
        else
        {
            Console.WriteLine("Невiрний формат вводу для максимальної кiлькостi запитiв.\nВстановлено кiлькiсть 2 (за замовченням).");
            maxRequest = 2;
         }
        
        Console.WriteLine("Введiть кiлькiсть нових клiєнтiв:");
        if (int.TryParse(Console.ReadLine(), out int newClientInput)) 
        { newClient = newClientInput; }
        else
        {
            Console.WriteLine("Невiрний формат вводу для кiлькостi нових клiєнтiв.\nВстановлено кiлькiсть 2 (за замовченням).");
            newClient = 2;
        }
       
            for (int i = 0; i < newClient; i++)
            {
                Console.WriteLine($"Введiть логiн для {i+1}-го користувача:");
                string username = Console.ReadLine();
                Console.WriteLine($"Введiть пароль для {i+1}-го користувача:");
                string password = Console.ReadLine();
                try { userList.Add(username, password); } 
                catch { Console.WriteLine("Користувач з таким iм'ям вже iснує!"); }
            }
      
        Console.WriteLine("\nВведенi данi:");
        Console.WriteLine($"Максимальна кiлькiсть клiєнтiв: {maxClients}");
        Console.WriteLine($"Максимальна кiлькiсть запитiв: {maxRequest}");
        Console.WriteLine("Логiни та паролi користувачiв:");
        foreach (var user in userList)
        {
            Console.WriteLine($"{user.Key}: {user.Value}");
        }
    
    IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        var ipEndPoint = new IPEndPoint(ipAddress, 8000);
        TcpListener server = new TcpListener(ipEndPoint);

        try
        {
            server.Start();
            Console.WriteLine("Сервер працює\n");
            Console.WriteLine("Очiкування пiдключення клiєнта...");

            while (true)
            {
                if (countClient < maxClients)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    var clientHandler = new ClientHandler(client);
                    _ = clientHandler.HandleClientAsync();
                    countClient++;
                }
                else
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    NetworkStream stream = client.GetStream();
                    byte[] response;
                    response = Encoding.Unicode.GetBytes("Ліміт підключень перевищено!!! Спробуйте пізніше.");
                    await stream.WriteAsync(response, 0, response.Length);
                    client.Close();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Помилка: {e.Message}");
        }
        finally
        {
            server?.Stop();
        }
    }
    

    class ClientHandler
    {
        private TcpClient client;
        private int countRequest = 0;

        public ClientHandler(TcpClient client)
        {
            this.client = client;
        }

        public async Task HandleClientAsync()
        {
            DateTime connectionTime = DateTime.Now;
            Console.WriteLine($"Клiєнт {((IPEndPoint)client.Client.RemoteEndPoint).Port} пiдключився: {connectionTime}");
            string logMessage1 = $"Клiєнт {((IPEndPoint)client.Client.RemoteEndPoint).Port} | Час: {DateTime.Now} | пiдключився.";
            LogToFile("log.txt", logMessage1);
            
            try
            {
                NetworkStream stream1 = client.GetStream();
                byte[] data = new byte[256];
                int bytes = await stream1.ReadAsync(data, 0, data.Length);
                string[] logPass = Encoding.Unicode.GetString(data, 0, bytes).Split(' ');

                if (CheckUser(logPass[0], logPass[1]))
                {
                    byte[] response;
                    if (countClient < maxClients) { response = Encoding.Unicode.GetBytes("Підключено до сервера"); }
                    else { response = Encoding.Unicode.GetBytes("Ліміт підключень перевищено! Спробуйте пізніше."); }
                    await stream1.WriteAsync(response, 0, response.Length);
                    while (true)
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] data1 = new byte[256];
                        StringBuilder builder = new StringBuilder();
                        int bytes1 = 0;
                            do
                            {
                                bytes = await stream.ReadAsync(data, 0, data.Length);
                                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                            }
                            while (stream.DataAvailable);
                        if (countRequest == maxRequest)
                        {
                            byte[] mess = Encoding.Unicode.GetBytes("Кiлькiсть запитiв перевищено. Спробуйте через хвилину!");
                            await stream.WriteAsync(mess, 0, mess.Length);
                            Console.WriteLine($"Клiєнт {((IPEndPoint)client.Client.RemoteEndPoint).Port} вiдключився: {DateTime.Now}");
                            string logMessage2 = $"Клiєнт {((IPEndPoint)client.Client.RemoteEndPoint).Port} | Час: {DateTime.Now} | вiдключився.";
                            LogToFile("log.txt", logMessage2);
                            client?.Close();
                            countClient--;
                        }
                        else
                        {
                            string[] currencies = builder.ToString().Split(' ');
                            string result = GetCurrencyRate(currencies[0], currencies[1]);
                            Console.WriteLine($"Курс валют: {builder} = {result}");
                            string logMessage = $"Клiєнт {((IPEndPoint)client.Client.RemoteEndPoint).Port} | Час: {DateTime.Now} | {builder} | Курс: {result}";
                            LogToFile("log.txt", logMessage);
                            byte[] response1 = Encoding.Unicode.GetBytes(result);
                            await stream.WriteAsync(response1, 0, response1.Length);
                            countRequest++;
                        }
                    }
                }
                else
                {
                    byte[] response; response = Encoding.Unicode.GetBytes("Автентифiкацiю не пройдено. Помилковий логiн або пароль!");
                    await stream1.WriteAsync(response, 0, response.Length);
                }
            }
            finally
            {
                Console.WriteLine($"Клiєнт {((IPEndPoint)client.Client.RemoteEndPoint).Port} вiдключився: {DateTime.Now}");
                string logMessage = $"Клієнт {((IPEndPoint)client.Client.RemoteEndPoint).Port} | Час: {DateTime.Now} | відключився.";
                LogToFile("log.txt", logMessage);
                client?.Close();
                countClient--;
            }
        }

        private static string GetCurrencyRate(string currency1, string currency2)
        {
            switch (currency1.ToUpper())
            {
                case "USD":
                    switch (currency2.ToUpper())
                    {
                        case "EURO":
                            return "0.8";
                        case "UAH":
                            return "36,54";

                        default:
                            return "1";
                    }
                case "EURO":
                    switch (currency2.ToUpper())
                    {
                        case "USD":
                            return "1.2";
                        case "UAH":
                            return "39.62";

                        default:
                            return "1";
                    }
                case "UAH":
                    switch (currency2.ToUpper())
                    {
                        case "USD":
                            return "0.027";
                        case "EURO":
                            return "0.025";
                        default:
                            return "1";
                    }
                default:
                    return "1";
            }
        }
    }
    private static void LogToFile(string filePath, string logMessage)
    {
        try
        {
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(logMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка запису в файл: {ex.Message}");
        }
    }
    private static bool CheckUser(string login, string password)
    {
        if (userList.ContainsKey(login) && userList[login] == password)
        {
            return true; 
        }
        else
        {
            return false; 
        }
    }

}

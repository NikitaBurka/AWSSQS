using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSSQS_example.Models;

namespace AWSSQS_example
{
    internal class Program
    {
      
        static string accessKey = "AKIASX5D2DZGJTQWZHPH";
        static string secretKey = "j8Uj5ncpYzPD3fc1ICEuhIRq8tV6yitdqf/yHLbA";
        static string queueUrl = "https://sqs.eu-central-1.amazonaws.com/188785237580/sqsexample2";

        static AmazonSQSClient sqs = new AmazonSQSClient(accessKey, secretKey, RegionEndpoint.EUCentral1);
        static int reqId = 1;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            while (true)
            {
                Console.WriteLine("\n--- МЕНЮ ---");
                Console.WriteLine("1 - Створити заявку");
                Console.WriteLine("2 - Обробити всі заявки");
                Console.WriteLine("3 - Показати кількість заявок у черзі");
                Console.WriteLine("0 - Вихід");
                Console.Write("Ваш вибір: ");

                string choice = Console.ReadLine();
                Console.WriteLine();

                if (choice == "1")
                {
                    await SendRequest();
                }
                else if (choice == "2")
                {
                    await ProcessRequests();
                }
                else if (choice == "3")
                {
                    await GetQueueCount();
                }
                else if (choice == "0")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Невірний вибір!");
                }
            }
        }

        static async Task SendRequest()
        {
            Console.Write("Введіть ім'я: ");
            string name = Console.ReadLine();

            Console.Write("Введіть тему: ");
            string topic = Console.ReadLine();

            Console.Write("Введіть опис: ");
            string desc = Console.ReadLine();

            Console.WriteLine("Пріоритет (1 - Low, 2 - Medium, 3 - High): ");
            string pChoice = Console.ReadLine();

            Priority p = Priority.Low;
            if (pChoice == "2") p = Priority.Medium;

            if (pChoice == "3") p = Priority.High;

            var req = new SupportRequest
            {
                Id = reqId++,
                UserName = name,
                Topic = topic,
                Description = desc,
                Priority = p,
                CreatedAt = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(req);
            await sqs.SendMessageAsync(queueUrl, json);

            Console.WriteLine($"Заявка №{req.Id} успішно додана в чергу!");
        }

        static async Task ProcessRequests()
        {
            int count = 0;

            while (true)
            {
                var receiveReq = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 2
                };

                var response = await sqs.ReceiveMessageAsync(receiveReq);

                if (response.Messages == null || response.Messages.Count == 0)
                {
                    if (count == 0)
                    {
                        Console.WriteLine("Черга порожня.");
                    }
                    break;
                }

                foreach (var msg in response.Messages)
                {
                    var data = JsonSerializer.Deserialize<SupportRequest>(msg.Body);
                    if (data != null)
                    {
                        TimeSpan waitTime = DateTime.UtcNow - data.CreatedAt;

                        Console.WriteLine("----------------------------------");
                        if (data.Priority == Priority.High)
                        {
                            Console.WriteLine("УВАГА: Термінова заявка!");
                        }

                        Console.WriteLine($"Заявка №: {data.Id}");
                        Console.WriteLine($"Користувач: {data.UserName}");
                        Console.WriteLine($"Тема: {data.Topic}");
                        Console.WriteLine($"Опис: {data.Description}");
                        Console.WriteLine($"Пріоритет: {data.Priority}");
                        Console.WriteLine($"Час очікування: {waitTime.Minutes} хв {waitTime.Seconds} сек");

                        await sqs.DeleteMessageAsync(queueUrl, msg.ReceiptHandle);
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                Console.WriteLine($"Оброблено заявок: {count}");
            }
        }

        static async Task GetQueueCount()
        {
            var req = new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string> { "ApproximateNumberOfMessages" }
            };

            var res = await sqs.GetQueueAttributesAsync(req);
            if (res.Attributes.TryGetValue("ApproximateNumberOfMessages", out string val))
            {
                Console.WriteLine($"Приблизна кількість заявок у черзі: {val}");
            }
        }
    }
}
using ClassLibrary1;
using Newtonsoft.Json;
using System;

namespace TestApplication
{
    class TestApplication
    {
        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ICache cache = new CacheClient();
            ICacheEvents cacheEvents = (ICacheEvents)cache;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Cache operations...");
                Console.WriteLine("1. Init");
                Console.WriteLine("2. Get");
                Console.WriteLine("3. Add");
                Console.WriteLine("4. Update");
                Console.WriteLine("5. Remove");
                Console.WriteLine("6. Clear");
                Console.WriteLine("7. Dispose Cache");
                Console.WriteLine("8. Add Serialized product object");
                Console.WriteLine("9. Get product object");
                Console.WriteLine("10. Subscribe to events");
                Console.WriteLine("11. UnSubscribe from events");
                int choice = int.Parse(Console.ReadLine());
                string key, value;

                switch (choice)
                {
                    case 1:
                        cache.Initialize();
                        break;
                    case 2:
                        Console.Write("Enter key: ");
                        key = Console.ReadLine();
                        string result = (string)cache.Get(key);
                        break;
                    case 3:
                        Console.Write("Enter key: ");
                        key = Console.ReadLine();
                        Console.Write("Enter value: ");
                        value = Console.ReadLine();
                        cache.Add(key, value, 500);
                        break;
                    case 4:
                        Console.Write("Enter key to update: ");
                        key = Console.ReadLine();
                        Console.Write("Enter value: ");
                        value = Console.ReadLine();
                        cache.Update(key, value, 100);
                        break;
                    case 5:
                        Console.Write("Enter key: ");
                        key = Console.ReadLine();
                        cache.Remove(key);
                        break;
                    case 6:
                        cache.Clear();
                        break;
                    case 7:
                        cache.Dispose();
                        break;
                    case 8:
                        Console.Write("Product Serialized Object inserting...");
                        cache.Add("product", JsonConvert.SerializeObject(CreateProduct()), 1000);
                        break;
                    case 9:
                        Console.Write("Get Product Object with key");
                        Console.Write("Enter key: ");
                        key = Console.ReadLine();
                        string myprod = (string)cache.Get(key);
                        Product myprodObj = JsonConvert.DeserializeObject<Product>(myprod);
                        Console.WriteLine(myprodObj.ToString());
                        break;
                    case 10:
                        cache.SubscribeToCacheUpdates(cacheEvents);
                        break;
                    case 11:
                        cache.UnsubscribeFromCacheUpdates(cacheEvents);
                        break;
                }
            }
        }

        /// <summary>
        /// Create new product using product class
        /// </summary>
        /// <returns></returns>
        private static Product CreateProduct()
        {
            return new Product
            {
                Name = "MYPROD",
                UnitPrice = 50,
                Category= "GARMENTS",
            };
        }

    }
}
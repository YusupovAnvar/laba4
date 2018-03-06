using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            Run();
            Console.WriteLine("Для завершения работы нажмите любую клавишу (тык)");
            Console.ReadKey();
        }

        static void Run()
        {
            List<Vacan> vacances = GetVacancies();
            Console.WriteLine("Зарплата >= 120000 рублей");
            foreach (var vac in vacances)
            {
                if (vac.Salary >= 120000)
                {
                    Console.WriteLine("Название профессии-->" + vac.Name);
                    Console.WriteLine("Зарплата->" + vac.Salary);
                    SetKeySkillsForVacancy(vac);
                    for (int i = 0; i < vac.KeySkills.Count; i++)
                    {
                        Console.WriteLine("Ключевой навык " + (i + 1) + "---> " + vac.KeySkills[i]);
                    }
                }
            }
            Console.WriteLine();

            Console.WriteLine("Зарплата < 15000 рублей");
            foreach (var vac in vacances)
            {
                if (vac.Salary < 15000)
                {
                    Console.WriteLine("Название профессии-->" + vac.Name);
                    Console.WriteLine("Зарплата->" + vac.Salary);
                    SetKeySkillsForVacancy(vac);
                    for (int i = 0; i < vac.KeySkills.Count; i++)
                    {
                        Console.WriteLine("Ключевой навык " + (i + 1) + "--->" + vac.KeySkills[i]);
                    }
                }
            }
            Console.WriteLine();
        }

        static void SetKeySkillsForVacancy(Vacan vacancy)
        {
            string vacResponse = SendRequest("https://api.hh.ru/", "vacancies/" + vacancy.Id).Result;

            dynamic vacResults = JsonConvert.DeserializeObject<dynamic>(vacResponse);
            if (vacResults.keyskills != null)
            {
                foreach (var keyskill in vacResults.keyskills)
                {
                    vacancy.KeySkills.Add((string)keyskill.name);
                }
            }
        }

        static List<Vacan> GetVacancies()
        {
            Console.WriteLine("Cбор данных...");
            List<Curren> curren = new List<Curren>();

            Console.WriteLine("Курса валют...");
            string curResponse = SendRequest("https://api.hh.ru/", "dictionaries").Result;
            dynamic curResults = JsonConvert.DeserializeObject<dynamic>(curResponse);
            if (curResults.currency != null)
            {
                foreach (var cur in curResults.currency)
                {
                    Curren cu = new Curren();
                    if (cur.code != null) cu.Code = (string)cur.code;
                    if (cur.rate != null) cu.Rate = (double)cur.rate;
                    curren.Add(cu);
                }
            }
            List<Vacan> vaca = new List<Vacan>();
            Console.WriteLine("Получение вакансий...");
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("Запрос 20 элементов с " + i + " страницы");
                string formattedResponse = SendRequest("https://api.hh.ru/", "vacancies?per_page=20&page=" + i).Result;
                dynamic result = JsonConvert.DeserializeObject<dynamic>(formattedResponse);
                if (result.items != null)
                {
                    foreach (var item in result.items)
                    {
                        if (item.salary != null)
                        {
                            Vacan v = new Vacan();
                            if (item.id != null) v.Id = (int)item.id;
                            if (item.name != null) v.Name = (string)item.name;

                            if (item.salary.from != null && item.salary.to != null)
                            {
                                v.Salary = ((int)item.salary.from + (int)item.salary.to) / 2;
                            }
                            else if (item.salary.from != null)
                            {
                                v.Salary = (int)item.salary.from;
                            }
                            else if (item.salary.to != null)
                            {
                                v.Salary = (int)item.salary.to;
                            }

                            if (item.salary.currency != null)
                                v.Salary = (int)(v.Salary * curren.Find(c => c.Code.Equals((string)item.salary.currency)).Rate);

                            vaca.Add(v);
                        }
                    }
                }
            }
            Console.WriteLine();

            return vaca;
        }

        static async Task<string> SendRequest(string uri, string requestUri)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(uri),
                Timeout = TimeSpan.FromSeconds(10)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "api-test-agent");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string formattedResponse = "";

            if (response.IsSuccessStatusCode)
            {
                formattedResponse = response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                Console.WriteLine("Ошибка запроса");
            }

            return formattedResponse;
        }
    }

    class Vacan
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Salary { get; set; }
        public List<string> KeySkills = new List<string>();
    }

    class Curren
    {
        public string Code { get; set; }
        public double Rate { get; set; }
    }
}

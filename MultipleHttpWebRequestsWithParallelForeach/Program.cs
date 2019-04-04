using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MultipleHttpWebRequestsWithParallelForeach
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("* Múltiplas requisições HTTP com Parallel.Foreach *");
            Console.WriteLine("\nO processo iniciou...");

            var cepsList = GetCepList(); // pego a lista de ceps através de algum input, tipo: texto, banco de dados e etc...

            var addressList = new List<Address>();

            var stopWatch = new Stopwatch(); //Fornece métodos para medir o tempo.

            stopWatch.Start();

            Parallel.ForEach(cepsList, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (cep) =>
             {
                 var Url = $"https://viacep.com.br/ws/{cep}/json/";

                 try // importante colocar um tratamento de exceção para não quebrar toda a iteração caso hava algum erro/exceção
                 {
                     string streamResponseAsJson = GetJsonResponseFromWebService(Url);

                     if (string.IsNullOrEmpty(streamResponseAsJson))
                         return;

                     var address = JsonConvert.DeserializeObject<Address>(streamResponseAsJson);

                     lock (addressList) // equanto a thread atual está incrementando este recuso compartilhado, nenhuma outra thread poderá fazer está operação.
                     {
                         addressList.Add(address);
                     }
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine($"Erro on cep - {cep} : {ex.Message}");

                     return; // continua a iteração, funciona como o continue do for/ foreach
                 }
             });

            stopWatch.Stop();

            Console.WriteLine($"\nTempo de execução: {stopWatch.Elapsed}");
            Console.WriteLine($"Quantidade de endereços importados: {addressList.Count()}");

            //após as iterações paralelas, faça o que quiser com a lista...
        }

        private static string GetJsonResponseFromWebService(string Url)
        {
            WebProxy proxy = new WebProxy();
            proxy.Address = new Uri("http://189.39.120.226:3128");

            var request = (HttpWebRequest)WebRequest.Create(Url);

            request.Proxy = proxy;

            request.ContentType = "application/json; charset=utf-8";

            var response = request.GetResponse() as HttpWebResponse;  // faço uma requisição ao webservice da Viacep

            using (var responseStream = response.GetResponseStream())
            {
                var reader = new StreamReader(responseStream, Encoding.UTF8);

                return reader.ReadToEnd();
            }
        }

        private static List<string> GetCepList()
        {
            var cepsFile = File.ReadAllLines("Ceps.txt");

            return new List<string>(cepsFile);
        }
    }
}

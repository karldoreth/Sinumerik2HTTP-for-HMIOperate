using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;
using System.IO;
using Siemens.Sinumerik.Operate.Services;

namespace CAP2HTTP
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                DebugToFile("Programmstart");
                String Prefix = "http://*:8080/";
                DebugToFile("Starte Listener");

                #region "Starte HTTP"
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(Prefix);
                listener.Start();
                DebugToFile("HTTP Einsatzbereit...");
                #endregion

                #region "Starte CAP"
                DataSvc m_DataSvcReadWrite = null;
                m_DataSvcReadWrite = new DataSvc();
                DebugToFile("CAP Einsatzbereit...");
                #endregion



                #region "Listenerschleife für Anfragen"
                while (true)
                {
                    // Wartet auf die Anfrage durch mich im Browser
                    // http://localhost:8080/?request=hallo)
                    // http://localhost:8080/?write=R-Parameter1&wert=150

                    //Meldung an Console in Gelb.
                    DebugToFile("Warte auf Anfrage...");

                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request; //Hier steht der Request drinnen
                    String Antwort = "";
                    DebugToFile("Abfrage");

                    try //Try für die CAP-Anfrage.
                    {
                        // In dem Objekt request wird auf die Anfrage url eingegangen
                        String Gets = request.Url.Query.Remove(0, 1);
                        // Konvertiert eine Zeichenfolge in eine Darstellung ohne Escapezeichen damit bestimmte Zeichen vernünftig dargestellt werden!
                        Gets = Uri.UnescapeDataString(Gets);

                        DebugToFile("Anfrage");

                        //Schreiben?
                        if (Gets.Split('=')[0] == "write")
                        {
                            //Korrekt aufteilen.
                            string[] Befehlsteile = Gets.Split('&');
                            string[] Variablenbefehl = Befehlsteile[0].Split('=');
                            string[] Wertbefehl = Befehlsteile[1].Split('=');
                            DebugToFile(" Schreibe");

                            //Schreibe über CAP
                            Item itemWrite = new Item();
                            itemWrite.Path = Variablenbefehl[1];
                            itemWrite.Value = Wertbefehl[1];
                            m_DataSvcReadWrite.Write(itemWrite);

                            //Antwort via http
                            Antwort = "true";
                        }
                        else if (Gets.Split('=')[0] == "request")
                        {
                            string[] Befehlsteile = Gets.Split('&'); //Wenn mehrere Variablen abgefragt werden.
                            foreach (String EinBefehlsteil in Befehlsteile)
                            {
                                string[] Lesebefehl = EinBefehlsteil.Split('=');
                                DebugToFile("Lese");

                                Item itemRead = new Item();
                                itemRead.Path = Lesebefehl[1];
                                m_DataSvcReadWrite.Read(itemRead);
                                Antwort = Antwort + itemRead.Value.ToString() + ";";
                            }
                        }

                    }

                    catch (DataSvcException ex)
                    {
                        // set status to statusbar
                        Antwort = ("DataSvcException: ErrorNr: " + ex.ErrorNumber.ToString() + " ; Message: " + ex.Message);
                        DebugToFile(Antwort);
                    }
                    catch (Exception e) //Bei einem Fehler wird diese auch über HTTP gepostet
                    {
                        DebugToFile(e.Message);
                        Antwort = e.Message;
                        DebugToFile(Antwort);
                    }


                    HttpListenerResponse response = context.Response;
                    response.AddHeader("Content-type", "text/html");
                    // In UTF8 decodieren der Antowrt und Bytes zerlegen. Die Zahl 29 ist 50 (=>2) und 57 (=>9)
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(Antwort);

                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                #endregion
            }
            catch (Exception e)
            {
                DebugToFile(e.Message);
            }
        }

        private static void DebugToFile(string text)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(text);
            Console.ResetColor();

            //Console.ReadLine();
            System.IO.StreamWriter SW = new StreamWriter("Debug.txt", true);
            string zeit = DateTime.Now.ToString();
            SW.WriteLine(zeit + "; " + text);
            SW.Close();

        }
    }

 
}

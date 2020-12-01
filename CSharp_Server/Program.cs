using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CSharp_Server{
    class Program{
        
        private readonly static int port = 8888;

        private static Market market;

        public static void Main(String[] args) {


            run();
        }

        private static void run(){
            TcpListener server = startServer();
            try{
                server.Start();
            }
            catch (SocketException e){
                Console.WriteLine("Unable to start server");
                Environment.Exit(1);
            }
            
            Console.WriteLine("Server running and waiting for connections....");

            Market market = new Market();
            Thread marketThread = new Thread(new ThreadStart(market.run));
            market.run();
            
            //TODO not handling multiple clients.
            while (true){
                try {
                    
                    TcpClient socket = server.AcceptTcpClient();
                    ClientHandler client = new ClientHandler(socket, market);
                    Thread clientThread = new Thread(new ThreadStart(client.run));
                   clientThread.Start();

                } catch (IOException e) {
                    Console.WriteLine("Error accepting client.");
                }

            }
        }

        private static void saveState(){
            //Save the connections and who owns the stock whenever a change has been made:
        }

        private static void readState(){

        }

        //Starts the server and returns a server socket object.
        private static TcpListener startServer(){
            TcpListener socket = null;
            try{
                socket = new TcpListener(IPAddress.Loopback, 8888);
            }catch (IOException e){
                Console.WriteLine("Error starting server");
                Environment.Exit(100);
            }
            return socket;
        }
    }
}
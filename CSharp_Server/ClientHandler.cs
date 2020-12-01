using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CSharp_Server{
    public class ClientHandler{ 
        private TcpClient socket;
    private Market market;
    private StreamWriter writer;
    private StreamReader reader;
    private String ID;
    private bool connected;


    public ClientHandler(TcpClient socket, Market market){
        this.socket = socket;
        this.market = market;

    }

    public bool isConnected(){
        return this.connected;
    }

    public void setConnected(bool status){
        this.connected = status;
        if (status == false){ //If incoming status is setting connection to false
            quit();
        }
    }

    public void sendMessage(String message){
        writer.WriteLine(message);
        writer.Flush();
    }

    public String getID(){
        return this.ID;
    }

    private int getBalance(){
        int items = market.getBalance(this);
        return items;
    }

    public String connectionsToString(){
        StringBuilder result = new StringBuilder("[CONN]");
        
        foreach(var entry in Market.clients){
            ClientHandler client = entry.Value;
            result.Append(" " + client.getID());
        }

        
        return result.ToString();
    }



    public void quit(){
        Console.WriteLine("User: " + this.getID() + " disconnected from server.");
        Market.disconnectClient(this);
        try {
            this.socket.Close();
        } catch (IOException e) {
            Console.WriteLine("Error closing socket");
        }
        this.connected = false;
        Thread.CurrentThread.Interrupt();
        Market.updateMarket("[CONN] " + this.getID() + " disconnected");
    }

    public void run() {

        try{
            
            
            NetworkStream stream = socket.GetStream();
            this.reader = new StreamReader(stream);
            this.writer = new StreamWriter(stream);
            
           
            
            String reconnection = "new connection"; //initiated to new connection

            try {
                reconnection = reader.ReadLine();
            }catch(IOException e){
                Console.WriteLine("Error");
            }


            //If server restarted, client will send message reconnection with their previous ID, otherwise they're sent a new ID.
            Console.WriteLine("reconection: " + reconnection);
            if (reconnection.Equals("reconnection")){
                this.ID = reader.ReadLine();
            }else if (reconnection.Equals("new connection")){
                Console.WriteLine("Reconnection");
                this.ID = Market.generateID().Replace("([ID])|\\s+", "");

                sendMessage("[ID] " + this.ID);
            }


            Market.updateIDFile(this.ID);

            /*
            if (!(reconnection.equals("new connection"))){ //user has reconnected
                this.ID = reconnection;
                Console.WriteLine("Client reconnected with ID: " + this.ID);
            }else{ //First time connection for user
                this.ID = Market.generateID();
                Console.WriteLine("Client connected with ID: " + this.ID);
                sendMessage(this.ID);
            }*/

            //Add user to the Hashmap of connected traders.
            Market.newConnection(this);


            this.setConnected(true);

            Console.WriteLine("User: " + this.getID() + " has connected to the server");
            Market.updateMarket("[NEW_CONN]User: " + this.getID() + " has connected to the server");
            String connectionsResponse; //Used to send connections status to client
            Stock stock = Market.getStock("sample stock");
            while (connected) {
                try{
                    String input = reader.ReadLine();
                    input = input.Replace("@", ""); //@ = ping signal and is not a valid character for any commands.
                    switch (Convert.ToString(input.ToLower())) {
                        case "balance":
                            if (getBalance() !=0){
                                sendMessage("[UPDATE]" + Convert.ToString(getBalance()));
                            }else{
                                sendMessage("[WARNING] You do not own any stock.");
                            }
                            break;
                        case "buy":
                            //TODO Might have to go back to Market.trade as error is client side rather than serverside.

                            bool success = Market.trade(stock.getOwner(), this, stock);
                            if (success){
                                sendMessage("[WARNING]Trade successful");
                            }else{
                                sendMessage("[WARNING]Trade unsucessful");
                            }
                            break;


                        case "sell":
                            String IDtoSellTo = reader.ReadLine();
                            IDtoSellTo = IDtoSellTo.Replace("@", ""); //if any ping requests got mixed with the stream
                            IDtoSellTo = IDtoSellTo.Replace(" ", ""); //if any ping requests got mixed with the stream
                            ClientHandler clientToSellTo = Market.getClient(IDtoSellTo);
                            Console.WriteLine("Client to sell to: " + clientToSellTo);
                            bool sellSuccess = false;
                            if (clientToSellTo != null){
                                if (stock.getOwner() != clientToSellTo){
                                    sellSuccess = Market.trade(this, clientToSellTo, stock);
                                }else{
                                    Console.WriteLine("Trade unsucessful");
                                    sendMessage("[WARNING] Trade unsucessful");
                                }
                            }else{
                                Console.WriteLine("ID to sell to: " + IDtoSellTo);
                                Console.WriteLine("Invalid client ID");
                                sendMessage("[WARNING]Invalid client ID to sell to");
                            }
                            if (sellSuccess){
                                Console.WriteLine("sending successul");
                                sendMessage("[UPDATE]Sell successful");
                            }
                            break;
                        case "status":
                            String message = "[UPDATE]Stock owned by trader: " + Market.getStock("sample stock").getOwner().getID();
                            Console.WriteLine("sending status message: " + message);
                            sendMessage(message);
                            break;
                        case "connections":
                            connectionsResponse = connectionsToString();
                            sendMessage(connectionsResponse);
                            break;
                        case "quit":
                            connected = false; // break out of while loop to catch statement: setConnected() runs the  quit() function, so should not be used here.
                            break;
                        case "@": //Single character sent from the client every 5 seconds to see if the connection is still alive.
                            Console.WriteLine("ping detected.");
                            break;
                        case "": //Single character which acts as a ping when connection has been established.
                            break;
                        default:
                            Console.WriteLine("error input");
                            Console.WriteLine("input: "  + input);
                            break;
                        }

                    //Handles client disconnecting
                }catch (IOException e){
                    this.setConnected(false);
                }

            }
        }catch (IOException e){
            Console.WriteLine("Error establishing I/O stream");
        }

    }
    }
}
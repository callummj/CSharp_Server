using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CSharp_Server{
    public class Market{
        public static Dictionary<string, ClientHandler> clients = new Dictionary<string, ClientHandler>();
        private static List<Stock> stock = new List<Stock>(); //https://stackoverflow.com/questions/3367524/c-sharp-objects-in-arraylists

        private static int lastID;

        //Where the last used ID will be saved
        private static readonly string idDir = "lastID.txt"; //Directory to look for ID file if server is started from Server.Main

        private static readonly string secondIdDir = "lastID.txt"; //Directory to look for ID file if server is started from ServerRestarter.Main (ProecessBuilder).

        private static readonly object fileLock = new object(); //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/lock-statement

        public Market(){
            Stock item = new Stock("sample stock");
            stock.Add(item);
            Market.lastID = initID();
        }


        public static void disconnectClient(ClientHandler disconnectingClient){
            clients.Remove(disconnectingClient.getID());

            
            if ((stock[0].getOwner() == disconnectingClient)){
                resetStock();
                Console.WriteLine("Stock is now unowned, waiting for next connection");
            }
        }

        //Returns stock based on stock name, returns null if stock doesnt exist
        public static Stock getStock(String name){

            stock[0].getName();
            
            for (int i = 0; i < stock.Count; i++){
                if (stock[i].getName() == name){
                    return stock[i];
                }
            }

            return null;
        }

        public static void newConnection(ClientHandler newClient){
            clients.Add(newClient.getID(), newClient);
            Stock sampleStock = stock[0];
            if ((clients.Count == 1) && (sampleStock.getOwner() == null)){
                sampleStock.setOwner(newClient);
            }
        }

        public static void updateIDFile(string ID){
            Console.WriteLine("update id file");
            lock (fileLock){

                try{
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(idDir, true))
                    {
                        ID = ID.Replace("([ID])|\\s+", "");
                        file.WriteLine(ID);
                        file.Close();
                    }
                }
                catch (IOException e){
                    try{
                        using (System.IO.StreamWriter file =
                            new System.IO.StreamWriter(idDir, true))
                        {
                            ID = ID.Replace("([ID])|\\s+", "");
                            file.WriteLine(ID);
                            file.Close();
                        }
                    }
                    catch (IOException error){
                        Console.WriteLine("Second dir didnt work");
                        //todo create file
                    }
                }
            }
        }

        private static string createIDFile(){
            Console.WriteLine("create id file");

            lock (fileLock){
                try{
                    

                    // Append text to an existing file named "WriteLines.txt".
                    using (StreamWriter outputFile = new StreamWriter(idDir, false))
                    {
                        outputFile.WriteLine("1");
                        outputFile.Close();
                    }
                    
                    
                }
                catch (IOException e){
                    try{
                        
                        // Append text to an existing file named "WriteLines.txt".
                        using (StreamWriter outputFile = new StreamWriter(secondIdDir, false))
                        {
                            outputFile.WriteLine("1");
                            outputFile.Close();
                        }
                        
                    }
                    catch (IOException e2){
                        Console.WriteLine("Error creating save data creatIDFile func");
                    }
                }
            }

            return "1"; //1 is the first ID generated
        }

        //When the program is ran, the server gets the last saved id
        private static int initID(){

            lock (fileLock){
                string userdata = null;
                string ID = "";
                try
                {
                    // Open the text file using a stream reader.
                    using (var sr = new StreamReader(idDir))
                    {
                        // Read the stream as a string, and write the string to the console.
                        userdata = sr.ReadToEnd();
                        sr.Close();
                    }
                }
                catch (FileNotFoundException error)
                {
                    try
                    {
                        using (var sr = new StreamReader(secondIdDir))
                        {
                            userdata = sr.ReadToEnd();
                            sr.Close();
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("ID file does not exist, creating file");
                        ID = createIDFile();
                        return Int32.Parse(ID);
                    }
                }
                
                ID = ID.Replace("([ID])|\\s+", "");
                return Int32.Parse(ID);
            }
            
            
            
        }

        public static String generateID(){
            lock (fileLock){
                
                lastID++;
                String idToUpdate = Convert.ToString(lastID);
                idToUpdate = idToUpdate.Replace(" ", ""); //in case of any
                Boolean idValid = false;
    
                //Make sure that the id hasn't already been assigned to an online client (could occur if server restarted.
                while (!idValid){
                    foreach(var entry in clients){
                        ClientHandler client = entry.Value;
                        if (client.getID().Equals(idToUpdate)){ 
                            lastID++;
                            idToUpdate = Convert.ToString(lastID);
                        }
                    }
                    idValid = true;
                }
    
                updateIDFile(idToUpdate);
                return Convert.ToString(lastID);
            }
            
        }
         public static ClientHandler getClient(String ID) {
        return clients[ID];
    }


    //Returns a total of stock owned by client.
    public int getBalance(ClientHandler client) {
        int total = 0;
        for (int i = 0; i < stock.Count; i++) {
            if ((stock[i].getOwner()) == client) {
                total++;
            }
        }
        return total;
    }



    //TODO doesnt work: may have to sync threads.
    public static Boolean trade(ClientHandler oldOwner, ClientHandler newOwner, Stock stock) {

        Console.WriteLine(newOwner.getID() + " is in trade");

        // UNCOMMENT FOR TESTING IF A TRADER DISCONNECTS MID-TRADE
/*
        try {
            Thread.sleep(10000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
*/

        lock (fileLock){
            //ClientHandler oldOwner = stock.getOwner();
            if (oldOwner == stock.getOwner()){
                //Check if the owner is also the buyer
                if (stock.getOwner() == newOwner) {
                    Console.WriteLine("stock owner: " + stock.getOwner().getID());
                    Console.WriteLine("new owner: " + newOwner.getID());
                    newOwner.sendMessage("Buy/Sell failed: cannot sell to owner.");
                    Console.WriteLine("There was an attempted trade of: " + stock.getName() + " but failed, because the owner tried to trade with themselves.");
                    return false;
                } else {
                    stock.setOwner(newOwner);
                    Console.WriteLine("new owner: " + stock.getOwner().getID());
                    //Check that new owner has been assigned
                    if (stock.getOwner() == newOwner) {
    
                        if ((oldOwner.isConnected()) && (newOwner.isConnected())) { //Check if traders are still connected before finalisation of trade.
                            String updateMsg = "[UPDATE]Stock is now owned by trader: " + Market.getStock("sample stock").getOwner().getID();
                            updateMarket(updateMsg);
                            return true;
                        } else {
                            stock.setOwner(oldOwner);
                            Console.WriteLine("new owner offline");
                            return false;
                        }
                    }else{
                        Console.WriteLine("Stock owner was unable to be changed.");
                        return false;
                    }
                }
            }else{
                Console.WriteLine("client: " + oldOwner.getID() + " tried to sell the stock, but are not the owner.");
                return false;
            }
        }

        


    }

   

    //Updates the users of the current market
    public static void updateMarket(String message){
        object lockObject = new Object();

        /*
        if (!(message.startsWith("[UPDATE]"))){
            if (!(message.startsWith("[CONN]"))){
                message = "[UPDATE] " + message;
            }
        }*/
        Console.WriteLine("sending update to clients. Message: " + message);
        lock (lockObject){

            foreach(var entry in clients){
                ClientHandler client = entry.Value; 
                Console.WriteLine("Sending to client: "+client.getID());
                client.sendMessage(message);
            }
        }
        

    }

    public static void resetStock(){
        Stock stock = getStock("sample stock");
        object lockObject = new Object();
        lock (lockObject){
            if (clients.Count == 0){
                stock.setOwner(null);
            }else{
          
    
                Boolean getClient=true;
    
                
                foreach(var entry in clients){
                    ClientHandler client = entry.Value;
                    stock.setOwner(client); 
                    getClient = false;
                }
                
            
    
            }
            String updateMsg = "[UPDATE]Previous owner disconnected, stock is now owned by trader: " + Market.getStock("sample stock").getOwner().getID();
            updateMarket(updateMsg);
        }
        
    }

    
    public void run() {
    }

    }
}
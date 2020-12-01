namespace CSharp_Server{
    public class Stock{
        private string name;
        private ClientHandler owner;

        public Stock(string name){
            this.name = name; owner = null;
        }

        public string getName(){return this.name;}

        public ClientHandler getOwner(){return this.owner;}

        public void setOwner(ClientHandler newOwner){this.owner = newOwner;}


        public bool hasOwner(){
            if (this.owner == null){
                return false;
            }else{
                return true;
            }
        }
    }
}
namespace QFramework.Gameplay
{
    public class BuyCommand : AbstractCommand
    {
        int cost = 0;
        public BuyCommand(int cost)
        {
            this.cost = cost;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<GameModel>();
            if(model.Money.Value >= cost)
            {
                model.Money.Value -= cost;
            }
            else return;

            model.Attack.Value += 1;
        }
    }
}
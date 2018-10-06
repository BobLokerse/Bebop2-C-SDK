using System;

namespace drone_UDP
{
    public static class Program
    {
        public static void Main(string[] args)
        {

            //This is a sample about using the pilotting command.

            var bebop = new BebopCommand();
            if (bebop.Discover() == -1)
            {
                Console.ReadLine();
                return;
            }

            while (true)
            {
                if (!Pilotting.ExecuteCommandOnUserInput(bebop))
                {
                    return;
                }
            }
        }
    }
}

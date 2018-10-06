using System;

namespace drone_UDP
{
    public static class Pilotting
    {
        /// <summary>
        /// Reads the user input and executes the command.
        /// </summary>
        /// <param name="bebop">An instance of <see cref="BebopCommand"/>.</param>
        /// <returns>false if quiting.</returns>
        public static bool ExecuteCommandOnUserInput(BebopCommand bebop)
        {
            var input = Console.ReadLine();
            switch (input)
            {
                //takeoff
                case "t":
                    bebop.Takeoff();
                    break;
                //landing
                //moving command: -100% ~ 100%
                case "l":
                    bebop.Landing();
                    break;
                //left
                case "a":
                    bebop.Move(1, -10, 0, 0, 0);
                    break;
                //right
                case "d":
                    bebop.Move(1, 10, 0, 0, 0);
                    break;
                //forward
                case "w":
                    bebop.Move(1, 0, 10, 0, 0);
                    break;
                //backward
                case "s":
                    bebop.Move(1, 0, -10, 0, 0);
                    break;
                //turn left
                case "h":
                    bebop.Move(0, 0, 0, -10, 0);
                    break;
                //turn right
                case "k":
                    bebop.Move(0, 0, 0, 10, 0);
                    break;
                //up
                case "u":
                    bebop.Move(0, 0, 0, 0, 10);
                    break;
                //down
                case "j":
                    bebop.Move(0, 0, 0, 0, -10);
                    break;
                //pause
                case "p":
                    bebop.Move(0, 0, 0, 0, 0);
                    break;
                case "v":
                    bebop.VideoEnable(); //enable RTP/.H264 videostreaming
                    break;
                //quit
                case "q":
                    bebop.CancleAllTask();
                    return false;
                default:
                    Console.WriteLine("Invalid command, try again.");
                    break;
            }

            return true;
        }
    }
}
// Paula Sierra Luque
// Cynthia Tristán Álvarez

using System;

namespace FPII23_P1_Naves
{
    internal class Program
    {
        static Random rnd = new Random(); // un único generador de aleaotorios para todo el programa
        const bool DEBUG = true; // para sacar información adicional en el Render
        const int ANCHO = 20,
                  ALTO = 16,  // área de juego
                  MAX_BALAS = 5,
                  MAX_ENEMIGOS = 9;

        // Evitar Console.Clear en Windows

        struct Tunel
        {
            public int[] suelo, techo;
            public int ini;
        }

        struct Entidad
        {
            public int fil, col;
        }

        struct GrEntidades
        {
            public Entidad[] ent;
            public int num;
        }

        static void IniciaTunel(out Tunel tunel)
        {
            // creamos arrays
            tunel.suelo = new int[ANCHO];
            tunel.techo = new int[ANCHO];

            //tunel.ini = 4;
            //tunel.techo = new int[] { 1, 1, 2, 1, 0 };
            //1tunel.suelo = new int[] { 3, 3, 4, 3, 4 };

            // rellenamos posicion 0 como semilla para generar el resto
            tunel.techo[0] = 0;
            tunel.suelo[0] = ALTO - 1;

            // dejamos 0 como la última y avanzamos hasta dar la vuelta
            tunel.ini = 1;
            for (int i = 1; i < ANCHO; i++)
            {
                AvanzaTunel(ref tunel);
            }
            // al dar la vuelta y quedará tunel.ini=0    
        }

        static void AvanzaTunel(ref Tunel tunel)
        {
            // ultima pos del tunel: anterior a ini de manera circular
            int ult = (tunel.ini + ANCHO - 1) % ANCHO;

            // valores de suelo y techo en la última posicion
            int s = tunel.suelo[ult],
                t = tunel.techo[ult]; // incremento/decremento de suelo/techo

            // generamos nueva columna a partir de esta última
            int opt = rnd.Next(5); // obtenemos un entero de [0,4]
            if (opt == 0 && s < ALTO - 1) { s++; t++; }   // tunel baja y mantiene ancho
            else if (opt == 1 && t > 0) { s--; t--; }   // sube y mantiene ancho
            else if (opt == 2 && s - t > 7) { s--; t++; } // se estrecha (como mucho a 5)
            else if (opt == 3)
            {                    // se ensancha, si puede
                if (s < ALTO - 1) s++;
                if (t > 0) t--;
            } // con 4 sigue igual

            // guardamos nueva columna del tunel generada
            tunel.suelo[tunel.ini] = s;
            tunel.techo[tunel.ini] = t;

            // avanzamos la tunel.ini: siguiente en el array circular
            tunel.ini = (tunel.ini + 1) % ANCHO;
        }

        static char LeeInput()
        {
            char ch = ' ';
            if (Console.KeyAvailable)
            {
                string dir = Console.ReadKey(true).Key.ToString();
                if (dir == "A" || dir == "LeftArrow") ch = 'l';
                else if (dir == "D" || dir == "RightArrow") ch = 'r';
                else if (dir == "W" || dir == "UpArrow") ch = 'u';
                else if (dir == "S" || dir == "DownArrow") ch = 'd';
                else if (dir == "X" || dir == "Spacebar") ch = 'x'; // bala        
                else if (dir == "P") ch = 'p'; // pausa					
                else if (dir == "Q" || dir == "Escape") ch = 'q'; // salir
                while (Console.KeyAvailable) Console.ReadKey();
            }
            return ch;
        }

        static void RenderTunel(Tunel tunel)
        {
            Console.SetCursorPosition(0, 0);
            if (DEBUG)
            {
                Console.WriteLine(string.Join(" ", tunel.techo));
            }
            //DEBUG ↑ ↑ ↑ ↑ ↑ ↑ 
            for (int j = 0; j < ALTO; j++)
            {
                for (int i = 0; i < ANCHO; i++)
                {
                    if (j <= tunel.techo[(tunel.ini + i) % ANCHO] || j >= tunel.suelo[(tunel.ini + i) % ANCHO])
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                    else
                        Console.BackgroundColor = ConsoleColor.Black;
                    if (DEBUG)
                        Console.SetCursorPosition(i * 2, j + 1);
                    else
                        Console.SetCursorPosition(i * 2, j);
                    Console.Write("  ");
                }
            }

            //DEBUG ↓ ↓ ↓ ↓ ↓ ↓ ↓ 
            Console.BackgroundColor = ConsoleColor.Black;
            if (DEBUG)
            {
                Console.WriteLine();
                foreach (var item in tunel.suelo)
                {
                    Console.Write(" " + $"{item % 10}");
                }

                //Console.WriteLine("\n" + string.Join("", tunel.suelo));
                Console.WriteLine("\nini: " + tunel.ini);
            }
        }

        static void Main(string[] args)
        {
            IniciaTunel(out Tunel tunel);
            RenderTunel(tunel);
            while (true) ;
            /*
            while (DEBUG)
            {
                AvanzaTunel(ref tunel);
                RenderTunel(tunel);
                Thread.Sleep(200);
            }
            */
        }
    }
}

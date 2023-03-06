// Paula Sierra Luque
// Cynthia Tristán Álvarez

using System;
using System.Media;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace FPII23_P1_Naves
{
    internal class Program
    {
        static readonly Random rnd = new Random(); // un único generador de aleaotorios para todo el programa
        static readonly SoundPlayer sfxPlayer = new SoundPlayer(); // para efectos de sonido: disparo y colision
        const bool DEBUG           = true,
                   MUSICA          = true,
                   SONIDOS         = true; // para sacar información adicional en el Render
        const int ANCHO            = 25,
                  ALTO             = 16,   // área de juego
                  MAX_BALAS        = 5,
                  MAX_ENEMIGOS     = 9;
        const string VLCPLAYER     = @"vlc.exe",   // ruta de vlcplayer
                     MUSIC_TRACK   = @"ost.wav",   // " música de fondo
                     SHOOT_SOUND   = @"shoot.wav", // " sonido de disparo
                     HIT_SOUND     = @"hit.wav",   // " sonido de colision
                     MENSAJE_PAUSA = "PAUSA",      // mensaje al estar pausado el juego
                     MENSAJE_FINAL = "El juego ha finalizado.", // mensaje al acabar el juego
                     ARCHIVO       = @"partida.txt"; // archivo de partida

        #region TIPOS
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
            public Entidad[] ent; // array de entidades
            public int num; // cantidad de entidades (o enemigos o balas) / la primera posicion libre
        }
        #endregion

        #region MÉTODOS
        static void IniciaTunel(out Tunel tunel)
        {
            // creamos arrays
            tunel.suelo = new int[ANCHO];
            tunel.techo = new int[ANCHO];

            // rellenamos posicion 0 como semilla para generar el resto
            tunel.techo[0] = 0;
            tunel.suelo[0] = ALTO - 1;

            // dejamos 0 como la última y avanzamos hasta dar la vuelta
            tunel.ini = 1;
            for (int i = 1; i < ANCHO; i++) AvanzaTunel(ref tunel);
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
                for (int i = 0; i < ANCHO; i++)
                    Console.Write(" " + tunel.techo[((tunel.ini + i) % ANCHO)] % 10);
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
                    Console.SetCursorPosition(i * 2, j + Convert.ToInt16(DEBUG)); // el convert es que como debug es un booleano,
                                                                                  // puede ser un +1 si salen los números arriba
                                                                                  // y añadirselo al cursor o no directamente.
                    Console.Write("  ");
                }
            }

            //DEBUG ↓ ↓ ↓ ↓ ↓ ↓ ↓ 
            Console.ResetColor();
            if (DEBUG)
            {
                Console.WriteLine();
                for (int i = 0; i < ANCHO; i++)
                    Console.Write(" " + tunel.suelo[((tunel.ini + i) % ANCHO)] % 10);
                Console.WriteLine("\nini: " + tunel.ini + " ");
            }
        }

        static void Render(Tunel tunel, Entidad nave, GrEntidades enemigos, GrEntidades balas, GrEntidades colisiones)
        {
            Console.CursorVisible = false;
            RenderTunel(tunel);
            if (DEBUG)
            {
                Console.WriteLine("nave.col: " + nave.col + ", nave.fil: " + nave.fil + "   ");
                Console.Write("enemigos.col: "); for (int i = 0; i < enemigos.num; i++) Console.Write(enemigos.ent[i].col + "   ");
                Console.Write("\nenemigos.fil: "); for (int i = 0; i < enemigos.num; i++) Console.Write(enemigos.ent[i].fil + "   ");
                Console.WriteLine("\nenemigos.num: " + enemigos.num);
                Console.WriteLine("balas.num: " + balas.num);
            }

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            // NAVE
            if (nave.fil >= 0) // hay que ver como optimizar este if, pero como el enunciado dice de usar nave.fil 
                               // como condicion para parar el juego pues lol jijjijiji
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.SetCursorPosition(nave.col * 2, nave.fil + Convert.ToInt16(DEBUG));
                Console.Write("=>");
            }

            // ENEMIGOS
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < enemigos.num; i++)
            {
                Console.SetCursorPosition(enemigos.ent[i].col * 2, enemigos.ent[i].fil + Convert.ToInt16(DEBUG));
                Console.Write("<>");
            }

            // BALAS
            Console.ForegroundColor = ConsoleColor.Magenta;
            for (int i = 0; i < balas.num; i++)
            {
                Console.SetCursorPosition(balas.ent[i].col * 2, balas.ent[i].fil + Convert.ToInt16(DEBUG));
                Console.Write("--");
            }

            // COLISIONES
            Console.ForegroundColor = ConsoleColor.Red;
            for (int i = 0; i < colisiones.num; i++)
            {
                Console.SetCursorPosition(colisiones.ent[i].col * 2, colisiones.ent[i].fil + Convert.ToInt16(DEBUG));
                Console.Write("**");
            }
            Console.ResetColor();
            Console.SetCursorPosition(0, 0);
        }

        static void AñadeEntidad(Entidad ent, ref GrEntidades gr)
        {
            gr.ent[gr.num] = ent;
            gr.num++;
        }

        static void EliminaEntidad(int i, ref GrEntidades gr) 
        {
            gr.ent[i] = gr.ent[gr.num - 1]; // la última entidad pasa a la posición i del array y num--.
            gr.num--;
        }

        static void AvanzaNave(char ch, ref Entidad nave)
        {
            switch (ch)
            {
                case 'l': // left
                    if (nave.col > 0)
                        nave.col--;
                    break;
                case 'r': // right
                    if (nave.col < ANCHO - 1)
                        nave.col++;
                    break;
                case 'u': // up
                    if (nave.fil > 0)
                        nave.fil--;
                    break;
                case 'd': // down
                    if (nave.fil < ALTO - 1)
                        nave.fil++;
                    break;
                default:
                    break;
            }
        }

        static void GeneraEnemigo(ref GrEntidades enemigos, Tunel tunel)
        {
            if (enemigos.num < MAX_ENEMIGOS)
            {
                int chance;
                if (DEBUG) chance = 0; // siempre genera si estamos en debug
                else chance = rnd.Next(0, 4); // 1 entre 4

                if (chance == 0) // 25% chance
                {
                    int ind = (tunel.ini + ANCHO - 1) % ANCHO; // la anterior a ini es la última renderizada
                    Entidad enemigo; // genera entidad ahí
                    enemigo.col = ANCHO; // a la derecha del todo
                    enemigo.fil = rnd.Next(tunel.techo[ind] + 1, tunel.suelo[ind] - 1); // entre techo y suelo
                    AñadeEntidad(enemigo, ref enemigos); // la añade al grupo
                }
            }
        }

        static void AvanzaEnemigo(ref GrEntidades enemigos)
        {
            for (int i = 0; i < enemigos.num; i++)
            {
                if (enemigos.ent[i].col <= 0) // si se sale, eliminalo
                    EliminaEntidad(i, ref enemigos);
                enemigos.ent[i].col--; // mov hacia la izq
            }
        }

        static void GeneraBala(ref GrEntidades balas, Entidad nave)
        {
            if (balas.num < MAX_BALAS && nave.col < ANCHO - 1)
            {
                Entidad bala;
                bala.col = nave.col;
                bala.fil = nave.fil;
                AñadeEntidad(bala, ref balas);
                Sonido(sfxPlayer, SHOOT_SOUND);
            }
        }

        static void AvanzaBalas(ref GrEntidades balas)
        {
            for (int i = 0; i < balas.num; i++)
            {
                if (balas.ent[i].col >= ANCHO - 1)
                    EliminaEntidad(i, ref balas);
                balas.ent[i].col++;
            }
        }

        static void ColNaveTunel(Tunel tunel, ref Entidad nave, ref GrEntidades colisiones)
        {
            int ind = (tunel.ini + nave.col) % ANCHO;
            if (nave.fil <= tunel.techo[ind] || nave.fil >= tunel.suelo[ind])
            {
                Entidad colision;
                colision.fil = nave.fil;
                colision.col = nave.col;
                AñadeEntidad(colision, ref colisiones);
                nave.fil = -1;
                Sonido(sfxPlayer, HIT_SOUND);
            }
        }

        static void ColBalasTunel(Tunel tunel, ref GrEntidades balas, ref GrEntidades colisiones)
        { 
            for (int i = 0; i < balas.num; i++)
            {
                int ind = (tunel.ini + balas.ent[i].col) % ANCHO; // posición respecto al array donde está la bala
                if (balas.ent[i].fil <= tunel.techo[ind] || balas.ent[i].fil >= tunel.suelo[ind])
                {                                             // Si está en techo o en suelo
                    if (balas.ent[i].fil <= tunel.techo[ind]) // Si está en techo
                    {
                        int j = balas.ent[i].fil + 1;
                        while (j <= tunel.techo[ind]) EfectoRoto(i, ref j, 1, balas, ref colisiones); // EXTENSION 2
                        tunel.techo[ind] = balas.ent[i].fil - 1; // todo lo de abajo a eso se rompe también
                    }
                    else                                      // Si no está en techo, está en suelo
                    {
                        int j = balas.ent[i].fil - 1;
                        while (j >= tunel.suelo[ind]) EfectoRoto(i, ref j, -1, balas, ref colisiones); // EXTENSION 2
                        tunel.suelo[ind] = balas.ent[i].fil + 1; // suelo = fila de la bala
                    }
                    Entidad colision;
                    colision.fil = balas.ent[i].fil;
                    colision.col = balas.ent[i].col;
                    AñadeEntidad(colision, ref colisiones);
                    EliminaEntidad(i, ref balas);
                    Sonido(sfxPlayer, HIT_SOUND);
                }
            }
        }

        static void ColNaveEnemigos(ref Entidad nave, ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            for (int i = 0; i < enemigos.num; i++)
            {
                if (nave.fil == enemigos.ent[i].fil && nave.col == enemigos.ent[i].col)
                {
                    Entidad colision;
                    colision.fil = enemigos.ent[i].fil;
                    colision.col = enemigos.ent[i].col;
                    AñadeEntidad(colision, ref colisiones);
                    EliminaEntidad(i, ref enemigos);
                    nave.fil = -1;
                    Sonido(sfxPlayer, HIT_SOUND);
                }
            }
        }

        static void ColBalasEnemigos(ref GrEntidades balas, ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            for (int i = 0; i < enemigos.num; i++)
            {
                for (int j = 0; j < balas.num; j++)
                {
                    if (enemigos.ent[i].fil == balas.ent[j].fil && enemigos.ent[i].col == balas.ent[j].col)
                    {
                        Entidad colision;
                        colision.fil = enemigos.ent[i].fil;
                        colision.col = enemigos.ent[i].col;
                        AñadeEntidad(colision, ref colisiones);
                        EliminaEntidad(i, ref enemigos);
                        EliminaEntidad(j, ref balas);
                        Sonido(sfxPlayer, HIT_SOUND);
                    }
                }
            }
        }

        static void Colisiones(ref Tunel tunel, ref Entidad nave, ref GrEntidades balas, ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            ColNaveTunel(tunel, ref nave, ref colisiones);
            ColBalasTunel(tunel, ref balas, ref colisiones);
            ColNaveEnemigos(ref nave, ref enemigos, ref colisiones);
            ColBalasEnemigos(ref balas, ref enemigos, ref colisiones);
        }

        static void EfectoRoto(int i, ref int j, int sentido, GrEntidades balas, ref GrEntidades colisiones) // EXTENSIÓN 2
        {
            Entidad efecto;
            efecto.col = balas.ent[i].col;
            efecto.fil = j;
            j += sentido;
            AñadeEntidad(efecto, ref colisiones);
        }

        static void Sonido(SoundPlayer player, string sonido)
        {
            if (SONIDOS)
            {
                player.SoundLocation = sonido;
                player.Play();
            }
        }

        static void Mensaje(string mensaje, bool pausa)
        {
            Console.SetCursorPosition(ANCHO / 2 * 2 - mensaje.Length / 2, ALTO / 2 - 1 + Convert.ToInt16(DEBUG));
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(mensaje);
            Console.ResetColor();
            if (pausa) Console.ReadKey(); // manera sencilla de hacer una pausa
        }

        static void Salvar(string file, Tunel tunel, Entidad nave, GrEntidades enemigos, GrEntidades balas)
        { // INVARIANTES: el archivo tendrá las mismas constantes, por lo que el tunel mide lo mismo de ancho y alto (y lo mismo con el max de balas y enemigos)
            StreamWriter sw = new StreamWriter(file);
            sw.WriteLine(nave.fil); // NAVE
            sw.WriteLine(nave.col);

            for (int i = 0; i < ANCHO; i++) // TUNEL
                sw.WriteLine(tunel.techo[i]); 
            for (int i = 0; i < ANCHO; i++)
                sw.WriteLine(tunel.suelo[i]); 
            sw.WriteLine(tunel.ini);

            for (int i = 0; i < MAX_ENEMIGOS; i++) // ENEMIGOS
            {
                sw.WriteLine(enemigos.ent[i].fil);
                sw.WriteLine(enemigos.ent[i].col);
            }
            sw.WriteLine(enemigos.num);

            for (int i = 0; i < MAX_BALAS; i++) // BALAS
            {
                sw.WriteLine(balas.ent[i].fil);
                sw.WriteLine(balas.ent[i].col);
            }
            sw.WriteLine(balas.num);
            sw.Flush(); 
            sw.Close();
        }

        static void Restaurar(string file, out Tunel tunel, out Entidad nave, out GrEntidades enemigos, out GrEntidades balas)
        { // INVARIANTES: el archivo tendrá las mismas constantes, por lo que el tunel mide lo mismo de ancho y alto (y lo mismo con el max de balas y enemigos)
            StreamReader sr = new StreamReader(file);
            tunel.suelo = new int[ANCHO];
            tunel.techo = new int[ANCHO];
            enemigos.ent = new Entidad[MAX_ENEMIGOS];
            balas.ent = new Entidad[MAX_BALAS];

            nave.fil = int.Parse(sr.ReadLine()); // NAVE
            nave.col = int.Parse(sr.ReadLine());

            for (int i = 0; i < ANCHO; i++) // TUNEL
                tunel.techo[i] = int.Parse(sr.ReadLine()); 
            for (int i = 0; i < ANCHO; i++)
                tunel.suelo[i]= int.Parse(sr.ReadLine()); 
            tunel.ini = int.Parse(sr.ReadLine());

            for (int i = 0; i < MAX_ENEMIGOS; i++) // ENEMIGOS
            {
                enemigos.ent[i].fil = int.Parse(sr.ReadLine());
                enemigos.ent[i].col = int.Parse(sr.ReadLine());
            }
            enemigos.num = int.Parse(sr.ReadLine());

            for (int i = 0; i < MAX_BALAS; i++) // BALAS
            {
                balas.ent[i].fil = int.Parse(sr.ReadLine());
                balas.ent[i].col = int.Parse(sr.ReadLine());
            }
            balas.num = int.Parse(sr.ReadLine());
            sr.Close();
        }
        #endregion

        static void Main()
        {
            Tunel tunel;                                // Bloque de inicialización de entidades
            Entidad nave;                                   
            GrEntidades enemigos;
            GrEntidades balas;
            GrEntidades colisiones; // las colisiones siempre son nuevas al ejecutar el juego
            colisiones.ent = new Entidad[ANCHO * ALTO]; // Cantidad de casillas posibles con colisión (?)
            colisiones.num = 0;

            if (File.Exists(ARCHIVO)) 
                Restaurar(ARCHIVO, out tunel, out nave, out enemigos, out balas);
            else
            {
                nave.col = ANCHO / 2;
                nave.fil = ALTO / 2;
                enemigos.ent = new Entidad[MAX_ENEMIGOS];
                enemigos.num = 0;
                balas.ent = new Entidad[MAX_BALAS];
                balas.num = 0;
                IniciaTunel(out tunel);
            }

            Process p; // Declaración del proceso para la música de fondo
            if (MUSICA) p = Process.Start(VLCPLAYER, "--qt-start-minimized --loop " + MUSIC_TRACK);
            //if (!DEBUG) Console.SetWindowSize(ANCHO * 2, ALTO);
            Render(tunel, nave, enemigos, balas, colisiones); // Render inicial
            while (nave.fil >= 0)                             // Bucle Principal
            {
                char ch = LeeInput();                           // Lectura de input
                AvanzaTunel(ref tunel);                         // Recorrido circular
                GeneraEnemigo(ref enemigos, tunel);             // Probabilidad de generar un enemigo
                AvanzaEnemigo(ref enemigos);                    // Avance de cada enemigo
                Colisiones(ref tunel, ref nave, ref balas, ref enemigos, ref colisiones);     // Colisiones generales
                if (ch == 'q')                                  // Si salir
                {
                    Salvar(ARCHIVO, tunel, nave, enemigos, balas);
                    nave.fil = -1;                    
                }
                else if (ch == 'p') Mensaje(MENSAJE_PAUSA, true); // Si pausar
                if (nave.fil >= 0)                                // segun enunciado
                {
                    AvanzaNave(ch, ref nave);                     // Movimiento de la nave
                    if (ch == 'x') GeneraBala(ref balas, nave);   // Generación de 1 bala
                    AvanzaBalas(ref balas);                       // Cada bala avanza
                    Colisiones(ref tunel, ref nave, ref balas, ref enemigos, ref colisiones); // Colisiones generales
                }
                Render(tunel, nave, enemigos, balas, colisiones); // Renderizado
                Thread.Sleep(100);                                // Velocidad de juego
                for (int i = 0; i < colisiones.num; i++) EliminaEntidad(i, ref colisiones);   // Limpieza de colisiones
            }
            Mensaje(MENSAJE_FINAL, false);                      // Mensaje final
            if (MUSICA && Process.GetProcessesByName("vlc").Length > 0) p?.Kill();                              // Fin de la música si el proceso de ésta no es nulo 
            while (true) ;                                      // Para que no se auto cierre el programa
        }
    }
}
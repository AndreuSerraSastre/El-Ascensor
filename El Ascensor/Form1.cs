using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace El_Ascensor
{
    public partial class ElAscensor : Form
    {

        //BASE DE CONOCIMIENTOS
        private int PisoActualD = 1; //Dato del piso actual
        private List<Peticion> paradas = new List<Peticion>();

        /* Paradas: son todas las paradas que se han introducido, tanto la de los pisos como los botones de los ascensores, por ejemplo:
         * 5⇵: Alguien ha pulsado el boton 5 en el ascensor
         * 5↓: En el piso 5, alguien ha pulsado el boton de bajar
         * 5↑: En el piso 5, alguien ha pulsado el boton de subir
         */

        private bool Sube_baja = false; //Indica si el ascensor sube (true) o baja (false)
        private bool EnMarcha = false; //Indica si el ascensor se esta moviendo (true, en este caso evaluar sube_baja) o no (false, no evaluar sube_baja)
        //FIN BASE DE CONOCIMINETOS

        private Hashtable paradasY = new Hashtable(); //Posicion Y de los pisos para saber donde pararse en cada parada, por ejemplo, piso 1 y:596px
        private Thread t; //El ascensor es un hilo independiente, si no lo fuera al pulsar los botones, estos se quedan esperando a que el ascensor acabe
        public ElAscensor()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            PisoActual.Text = PisoActualD.ToString(); //Imprimimos por pantalla el piso actual

            // Introducimos la informacion de las Ys en cada piso
            paradasY.Add(1, Constants.ypiso1);
            paradasY.Add(2, Constants.ypiso2);
            paradasY.Add(3, Constants.ypiso3);
            paradasY.Add(4, Constants.ypiso4);
            paradasY.Add(5, Constants.ypiso5);
            paradasY.Add(6, Constants.ypiso6);

            //Creamos thread del ascensor para que se pueda mover libremente
            t = new Thread(new ThreadStart(MoverAscensor));
            t.Start();

            //Este bool sirve para que desde el thread se pueda llamar a objetos creados por el programa principal
            CheckForIllegalCrossThreadCalls = false;

            //Al cerrar el form cerramos el thread
            this.FormClosed += Form1_FormClosed;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Cerramos el thread
            t.Abort();
        }

        //Funcion que se encarga de mirar continuamente la lista de paradas, si hay alguna parada, escogerá la que se adapte a sus necesidades
        //y se moverá hasta la Y que corresponda a esa parada
        private void MoverAscensor()
        {
            //El ascensor siempre esta esperando peticiones (siempre esta encendido)
            while (true)
            {
                //Si hay paradas se pone en marcha
                while (paradas.Count != 0)
                {
                   
                    EnMarcha = true;

                    //Selecciona el psio en base a la base de conocimientos
                    Peticion peticion = SeleccionarPiso();

                    //Mira hasta donde tiene que moverse
                    int objetivoY = (int)paradasY[peticion.Piso];

                    //Indicacion de cuanto falta para llegar al piso
                    ProgressBar(objetivoY);

                    //Si se ha pulsado el piso actual no hacer nada
                    if (AscensorImagen.Location.Y != objetivoY)
                    {
                        //ir subiendo o bajando hasta la Y
                        while (AscensorImagen.Location.Y != objetivoY)
                        {
                            if (AscensorImagen.Location.Y < objetivoY)
                            {
                                //Indicamos si sube o baja y actualizamos las luces de subir o bajar
                                Sube_baja = false;
                                ActualizarSube_Baja();
                                AscensorImagen.Location = new Point(AscensorImagen.Location.X, AscensorImagen.Location.Y + 1);
                            }
                            else
                            {
                                Sube_baja = true;
                                ActualizarSube_Baja();
                                AscensorImagen.Location = new Point(AscensorImagen.Location.X, AscensorImagen.Location.Y - 1);
                            }
                            progressBar.PerformStep();
                            //Esperar un tiempo x para que no suba muy rapido
                            System.Threading.Thread.Sleep(10);
                        }
                        ActualizarSube_Baja();
                    }
                    //Llega al piso y actualiza los pisos pendientes y se abre y cierra la puerta
                    LlegadaAPiso(peticion);
                    ActualizarPisosAVisitar();
                }
                EnMarcha = false;
                ActualizarSube_Baja();
                ProgressBar(0);
            }
        }

        //En esta funcion se aplica toda la logica de seleccionar el piso al subir o bajar, dependiendo del piso donde estas...
        private Peticion SeleccionarPiso()
        {
            Peticion objetivo = paradas[0]; //de momento escoge el primer piso TODO
            PisoObjetivo.Text = objetivo.Piso.ToString();
            PisoObjetivo.Refresh();
            return objetivo;
        }

        private void ActualizarSube_Baja()
        {
            if (!EnMarcha)
            {
                Sube.BackColor = Color.FromArgb(255, 227, 227, 227);
                Baja.BackColor = Color.FromArgb(255, 227, 227, 227);
            }
            else
            {
                if (Sube_baja)
                {
                    Sube.BackColor = Color.Green;
                    Baja.BackColor = Color.FromArgb(255, 227, 227, 227);
                }
                else
                {
                    Sube.BackColor = Color.FromArgb(255, 227, 227, 227);
                    Baja.BackColor = Color.Green;
                }
            }
            Sube.Refresh();
            Baja.Refresh();
        }

        //Imprime por pantalla los pisos
        private void ActualizarPisosAVisitar()
        {
            PisosAVisitar.Text = "{ ";
            foreach (Peticion piso in paradas)
            {
                PisosAVisitar.Text += piso.Piso.ToString();
                if (piso.Panel)
                {
                    PisosAVisitar.Text += "⇵ ";
                }
                else
                {
                    if (!piso.Sube_baja)
                    {
                        PisosAVisitar.Text += "↓ ";
                    }
                    else
                    {
                        PisosAVisitar.Text += "↑ ";
                    }
                }
            }
            PisosAVisitar.Text += "}";
            PisosAVisitar.Refresh();
        }

        private void LlegadaAPiso(Peticion actual)
        {
            paradas.Remove(actual);
            PisoActualD = actual.Piso;
            PisoActual.Text = PisoActualD.ToString();
            PisoActual.Refresh();
            AscensorImagen.BackgroundImage = Properties.Resources.AA;
            AscensorImagen.Refresh();
            System.Threading.Thread.Sleep(800);
            AscensorImagen.BackgroundImage = Properties.Resources.AC2;
            AscensorImagen.Refresh();
        }

        private void ProgressBar(int piso)
        {
            progressBar.Maximum = Math.Abs(piso - AscensorImagen.Location.Y);
            progressBar.Minimum = 0;
            progressBar.Value = 0;
            progressBar.Step = 1;
        }

        private void Boton1_Click(object sender, EventArgs e)
        {
            //Añadir una parada: el primer valor es el piso, el segundo si es el boton de subir(true) o el de bajar(false) y el tercero indica si el
            //boton es el de dentro del ascensor, en el caso de ser de dentro del ascensor no influye si es de subir o de bajar.
            paradas.Add(new Peticion(1, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, false, true));
            ActualizarPisosAVisitar();
        }

        private void Boton6_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(6, false, true));
            ActualizarPisosAVisitar();
        }

        private void down6_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(6, false, false));
            ActualizarPisosAVisitar();
        }

        private void down5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, false, false));
            ActualizarPisosAVisitar();
        }

        private void up5_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(5, true, false));
            ActualizarPisosAVisitar();
        }

        private void down4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, false, false));
            ActualizarPisosAVisitar();
        }

        private void up4_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(4, true, false));
            ActualizarPisosAVisitar();
        }

        private void down3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, false, false));
            ActualizarPisosAVisitar();
        }

        private void up3_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(3, true, false));
            ActualizarPisosAVisitar();
        }

        private void down2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, false, false));
            ActualizarPisosAVisitar();
        }

        private void up2_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(2, true, false));
            ActualizarPisosAVisitar();
        }

        private void up1_Click(object sender, EventArgs e)
        {
            paradas.Add(new Peticion(1, true, false));
            ActualizarPisosAVisitar();
        }

        private void ElAscensor_Load(object sender, EventArgs e)
        {

        }
    }
}

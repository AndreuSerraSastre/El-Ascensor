namespace El_Ascensor
{
    public class Peticion
    {
        public int Piso = 0;
        public bool Sube_baja = false;
        public bool Panel = false;

        public Peticion(int piso, bool sube_baja, bool panel)
        {
            this.Piso = piso;
            this.Sube_baja = sube_baja;
            this.Panel = panel;
        }
    }
}

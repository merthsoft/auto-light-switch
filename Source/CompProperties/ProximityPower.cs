namespace Merthsoft.AutoOnAutoOff.CompProperties {
    class ProximityPower : Verse.CompProperties {
        public ProximityPower() {
            this.compClass = typeof(Comp.ProximityPower);

            checkRate = 250;
            autoOn = true;
            autoOff = true;
            showOverrideButton = true;
            offWhenSleeping = true;
        }

        public int checkRate;
        public bool autoOn;
        public bool autoOff;
        public bool showOverrideButton;
        public bool offWhenSleeping;
    }
}

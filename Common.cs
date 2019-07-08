using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


public enum EventTypes { Unknown, UpdateValues, Multiple, Voltage, Friction, GetValues, SetVelocity, ShuntVelocity, RoadVelocity,
    SectionVelocity, TrackVelocity, Lights, Animation, Switch, PutValues, SHP }

public enum EventStates { Scan, Name, Type, Timing, MemCell, Value, Parameter, Comment, NonNecessaryValue, MultipleValue }

public enum SectionStates {
    BrakDanych, Rozprucie, UtrataKontroliPołożenia, Zajęty, PrzebiegCzasowy, PrzebiegPociągowy,
    PrzebiegManewrowy, DrogaOchronna, ZamknięcieOchronne, ZamknięcieIndywidulane, RejonManewrowy, NastawianieLokalne,
    StanPodstawowy, BezKontroli, SygnałZastępczy, UtwierdzenieWDrodzePrzebiegu, KasowanieDrogiOchronnej,
    KasowanieZamknięciaOchronnego, UtwierdzaniePrzebiegu, KasowanieUtwierdzaniaPrzebiegu, Utwierdzenie, KasowanieUtwierdzenia,
    ŻądanieUstawieniaKierunku, UstawionyKierunek, UstawionyKierunekPoTorzeNiewłaściwym, PodanySygnałWyjazdowy,
    PociągNaSzlaku, PrzygotowanieDoWyjazduNaSygnałZastępczy, StwierdzonyWjazdPociągu, StwierdzonyKoniecPociągu,
    DoraźnieStwierdzonyKoniecPociągu, ZwalnianieKierunku, NieprawidłowyStanBlokady, KasowaniePodanegoSygnałuWyjazdowego,
    KasowaniePociąguNaSzlaku, ZwolnienieSekcji, ZajęcieZewnętrzne, ZwolnienieZewnętrzne, ZwolnienieUtwierdzeniaWPrzebiegu
}

public enum PrzebiegType { NoType = 0, Manewrowy = 2, Pociągowy = 1, Oba = 3, Automatyczny = 4, SBL = 5 }

public enum PlusDirection { NoDir = 0, Wprost = 1, Bok = 2}

public enum DirectionsAndTypes { Prawo, Lewo, Prawy, Lewy, Normalny, Pochylony, Wprost, Bok, S, Tm, STm, To, Sp, Eap, Eac, Felb, C }

public enum Orders {
    zmk, ozmk, plus, minus, plbi, mibi, ksr, stop, ostop, zds, lok, olok, poc, man, stoj, sz, nsz, osz, zdp, zdm, zcz, ozcz,
    wbl, owbl, poz, po, ko, dpo, dko, dskp, nothing
}

public enum TrackType { None, Normal, Switch, Derail, Turntable, LineEnd, TrackEnd, Crossing }

namespace Trax {
    class Tools {

        #region Fileds
        private static StringComparison CI = StringComparison.InvariantCultureIgnoreCase; // case ignoring comparison type
        private static IFormatProvider FP = System.Globalization.CultureInfo.InvariantCulture; // needed to have C default decimal separator
        #endregion


        public static string ChangeParamsToText(string source, List<string> parameters) {
            //scan source
            int sourceLength = source.Length;
            int sourceLastIndex = sourceLength - 1;
            EventStates state = EventStates.Scan;
            Stack<EventStates> states = new Stack<EventStates>();
            string fragment = "";
            string param = "";
            char c;
            bool isParamStart = false, isParamEnd = false;
            bool isEnd = false;
            // analysis:
            for (int i = 0; i < sourceLength; i++) {
                c = source[i];
                isParamStart = c == '(';
                isParamEnd = c == ')';
                isEnd = i == sourceLastIndex; // identifier is considered ended after whitespace character, comment start or end of data
                switch (state) {
                    case EventStates.Scan:
                        if (isParamStart) {
                            param += c;
                            states.Push(state);
                            state = EventStates.Parameter;
                        }
                        else fragment += c;
                        break;
                    case EventStates.Parameter:
                        param += c;
                        if (isParamEnd) {
                            string n = "";
                            foreach (char c1 in param) {
                                if (c1 >= '0' && c1 <= '9') { n += c1; }
                            }
                            int cyf = int.Parse(n);
                            if (cyf <= parameters.Count)
                                fragment += parameters[cyf - 1];
                            else
                                fragment += param;
                            state = states.Pop();
                            param = "";
                        }
                        break;

                }
            }
            return fragment;
        }

        public static EventTypes GetEventTypeFromString(string type) {
            if (type.Equals("setvelocity", CI)) return EventTypes.SetVelocity;
            else if (type.Equals("shuntvelocity", CI)) return EventTypes.ShuntVelocity;
            else if (type.Equals("lights", CI)) return EventTypes.Lights;
            else if (type.Equals("updatevalues", CI)) return EventTypes.UpdateValues;
            else if (type.Equals("getvalues", CI)) return EventTypes.GetValues;
            else if (type.Equals("putvalues", CI)) return EventTypes.PutValues;
            else if (type.Equals("multiple", CI)) return EventTypes.Multiple;
            else if (type.Equals("voltage", CI)) return EventTypes.Voltage;
            else if (type.Equals("friction", CI)) return EventTypes.Friction;
            else if (type.Equals("animation", CI)) return EventTypes.Animation;
            else if (type.Equals("switch", CI)) return EventTypes.Switch;
            else if (type.Equals("trackvel", CI)) return EventTypes.TrackVelocity;
            else if (type.Equals("cabsignal", CI)) return EventTypes.SHP;
            else return EventTypes.Unknown;
        }

        public static Orders GetOrderFromString(string order) {
            if (order.Equals("zmk", CI)) return Orders.zmk;
            else if (order.Equals("ozmk", CI)) return Orders.ozmk;
            else if (order.Equals("plus", CI)) return Orders.plus;
            else if (order.Equals("minus", CI)) return Orders.minus;
            else if (order.Equals("plbi", CI)) return Orders.plbi;
            else if (order.Equals("mibi", CI)) return Orders.mibi;
            else if (order.Equals("ksr", CI)) return Orders.ksr;
            else if (order.Equals("stop", CI)) return Orders.stop;
            else if (order.Equals("ostop", CI)) return Orders.ostop;
            else if (order.Equals("zds", CI)) return Orders.zds;
            else if (order.Equals("lok", CI)) return Orders.lok;
            else if (order.Equals("olok", CI)) return Orders.olok;
            else if (order.Equals("poc", CI)) return Orders.poc;
            else if (order.Equals("man", CI)) return Orders.man;
            else if (order.Equals("stoj", CI)) return Orders.stoj;
            else if (order.Equals("sz", CI)) return Orders.sz;
            else if (order.Equals("nsz", CI)) return Orders.nsz;
            else if (order.Equals("osz", CI)) return Orders.osz;
            else if (order.Equals("zdp", CI)) return Orders.zdp;
            else if (order.Equals("zdm", CI)) return Orders.zdm;
            else if (order.Equals("zcz", CI)) return Orders.zcz;
            else if (order.Equals("ozcz", CI)) return Orders.ozcz;
            else if (order.Equals("wbl", CI)) return Orders.wbl;
            else if (order.Equals("owbl", CI)) return Orders.owbl;
            else if (order.Equals("poz", CI)) return Orders.poz;
            else if (order.Equals("po", CI)) return Orders.po;
            else if (order.Equals("ko", CI)) return Orders.ko;
            else if (order.Equals("dpo", CI)) return Orders.dpo;
            else if (order.Equals("dko", CI)) return Orders.dko;
            else if (order.Equals("dskp", CI)) return Orders.dskp;
            else return Orders.nothing;
        }

        public static PrzebiegType GetSignalTypeFromString(string type) {
            if (type.Equals("s", CI)) return PrzebiegType.Pociągowy;
            else if (type.Equals("tm", CI)) return PrzebiegType.Manewrowy;
            else if (type.Equals("stm", CI)) return PrzebiegType.Oba;
            else return PrzebiegType.NoType;
        }

        public static PlusDirection GetPlusDirectionFromString (string dir) {
            if (dir.Equals("wprost", CI)) return PlusDirection.Wprost;
            else if (dir.Equals("bok", CI)) return PlusDirection.Bok;
            else return PlusDirection.NoDir;

        }

        public static double RadianToDegree(double angle) {
            return angle * (180.0 / Math.PI);
        }
    }

}
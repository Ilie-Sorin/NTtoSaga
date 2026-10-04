namespace NttoSaga.Core.Models;

/// <summary>
/// Calculează TVA-ul corect de export: cota (TVA_ART) aplicată la valoarea de achiziție fără TVA
/// (aceeași bază folosită de câmpul VALOARE), rotunjit la 2 zecimale — NU valoarea brută importată
/// din vânzarea cu amănuntul (ValVatAmIesire), care nu are legătură cu baza de calcul a transferului
/// între gestiuni.
/// </summary>
public static class TvaCalculator
{
    /// <summary>Toate valorile monetare în bani (întreg); rezultatul este tot în bani.</summary>
    public static long CalculeazaTvaBani(int procentTva, long valoareFaraTvaBani)
    {
        var valoareLei = valoareFaraTvaBani / 100m;
        var tvaLei = Math.Round(procentTva * valoareLei / 100m, 2, MidpointRounding.AwayFromZero);
        return (long)Math.Round(tvaLei * 100m, 0, MidpointRounding.AwayFromZero);
    }
}

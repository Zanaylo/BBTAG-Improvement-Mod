namespace BBTAG_IM.Core.Hantei.Games;

public interface IHanteiGameFiles
{
    IEnumerable<string> Names { get; }

    bool Contains(string name);

    byte[] Read(string name);
}

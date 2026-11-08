using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Hitboxes.Editing;

namespace BBTAG_IM.Core.Porting;

public sealed record SheetImage(string Name, int Width, int Height);

public static class FrameJonbin
{
    private const string ImageExtension = ".bmp";
    private const int DrawnLayer = 0;
    private const int OriginX = 128;
    private const int OriginY = 224;
    private const int HurtBoxType = 0;
    private const int AttackBoxType = 1;
    private const int FirstHurtBox = 1;
    private const int LastHurtBox = 8;

    public static byte[] Build(Ha6Frame frame, Func<int, SheetImage?> images)
    {
        List<string> names = [];
        List<SpriteChunk> chunks = [];

        foreach (var layer in frame.Layers.Where(layer => !layer.UsesPat && layer.Sprite >= 0))
        {
            if (images(layer.Sprite) is not { } image)
                continue;

            var name = image.Name + ImageExtension;
            var index = names.IndexOf(name);

            if (index < 0)
            {
                index = names.Count;
                names.Add(name);
            }

            chunks.Add(Chunk(layer, image, index));
        }

        return Jonbin.Compose(names, chunks, Boxes(frame));
    }

    private static SpriteChunk Chunk(Ha6Layer layer, SheetImage image, int index) =>
        new(0, 0, image.Width, image.Height, OriginX - layer.X, layer.Y - OriginY, -image.Width, image.Height, DrawnLayer, index);

    private static List<JonbinBox> Boxes(Ha6Frame frame)
    {
        List<JonbinBox> boxes = [];

        foreach (var (_, box) in frame.Boxes.Where(entry => entry.Key is >= FirstHurtBox and <= LastHurtBox).OrderBy(entry => entry.Key))
            boxes.Add(Mirror(box, HurtBoxType));

        foreach (var (_, box) in frame.AttackBoxes.OrderBy(entry => entry.Key))
            boxes.Add(Mirror(box, AttackBoxType));

        return boxes;
    }

    private static JonbinBox Mirror(Ha6Box box, int type) => new(type, -box.X1, box.Y1, box.X1 - box.X2, box.Y2 - box.Y1);
}

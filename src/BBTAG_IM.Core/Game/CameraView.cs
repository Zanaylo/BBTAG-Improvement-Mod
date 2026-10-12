using System.Numerics;

namespace BBTAG_IM.Core.Game;

public sealed record CameraView(Matrix4x4 View, Matrix4x4 Projection);

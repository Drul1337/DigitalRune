// DigitalRune Engine - Copyright (C) DigitalRune GmbH
// This file is subject to the terms and conditions defined in
// file 'LICENSE.TXT', which is part of this source code package.

using System;
using System.Diagnostics;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;


namespace DigitalRune.Geometry.Meshes
{
  /// <summary>
  /// Possible results of a collinearity test.
  /// </summary>
  /// <remarks>
  /// This enumeration is no flags enumeration (see <see cref="FlagsAttribute"/>).
  /// </remarks>
  internal enum Collinearity
  {
    /// <summary>
    /// The object is not collinear. It is not known if it is in front or behind.
    /// </summary>
    NotCollinear,

    /// <summary>
    /// The object is in front.
    /// </summary>
    NotCollinearInFront,

    /// <summary>
    /// The object is behind.
    /// </summary>
    NotCollinearBehind,

    /// <summary>
    /// The object is collinear and before. For line segments this means, the tested object is on
    /// the line before the start of the line segment.
    /// </summary>
    CollinearBefore,

    /// <summary>
    /// The object is collinear and after. For line segments this means, the tested object is on the
    /// line after the end of the line segment.
    /// </summary>
    CollinearAfter,

    /// <summary>
    /// The object is collinear and contained.
    /// </summary>
    CollinearContained,

    /// <summary>
    /// The object is collinear and the it is not known if the object is before, contained or after.
    /// </summary>
    Collinear,
  }


  public partial class DcelMesh
  {
    /// <summary>
    /// Determines the collinearity between a point and an edge.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="edge">The edge.</param>
    /// <returns>
    /// The type of collinearity.
    /// </returns>
    internal static Collinearity GetCollinearity(Vector3F point, DcelEdge edge)
    {
      Vector3F v0 = edge.Origin.Position;
      Vector3F v1 = edge.Twin.Origin.Position;
      Vector3F segment = v1 - v0;
      float segmentLengthSquared = segment.LengthSquared;

      // Vector from segment to point.
      Vector3F v0ToPoint = point - v0;

      // Compute normal component of v0ToPoint (= v0ToPoint - "v0ToPoint projected on segment").
      float v0ToPointDotSegment = Vector3F.Dot(v0ToPoint, segment);
      Vector3F normalFromLineToPoint = v0ToPoint - v0ToPointDotSegment / segmentLengthSquared * segment;

      if (normalFromLineToPoint.LengthSquared > Numeric.EpsilonF * (1 + segmentLengthSquared))
      {
        // point is not on line.
        return Collinearity.NotCollinear;
      }

      if (v0ToPointDotSegment < -Numeric.EpsilonF * segmentLengthSquared)
        return Collinearity.CollinearBefore;
      if (v0ToPointDotSegment > (1 + Numeric.EpsilonF) * segmentLengthSquared)
        return Collinearity.CollinearAfter;

      return Collinearity.CollinearContained;
    }


    /// <summary>
    /// Determines the collinearity between a point and a face.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="face">The face.</param>
    /// <returns>The collinearity type.</returns>
    internal static Collinearity GetCollinearity(Vector3F point, DcelFace face)
    {
      float dummy;
      return GetCollinearity(point, face, out dummy);
    }


    /// <summary>
    /// Determines the collinearity between a point and a face.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="face">The face.</param>
    /// <param name="inFront">
    /// A value that is positive if the point is in front. And the value is proportional to the
    /// distance of the point from the edge.
    /// </param>
    /// <returns>The collinearity type.</returns>
    internal static Collinearity GetCollinearity(Vector3F point, DcelFace face, out float inFront)
    {
      Vector3F v0 = face.Boundary.Origin.Position;
      Vector3F normal = face.Normal;
      Vector3F v0ToPoint = point - v0;

      float dot = Vector3F.Dot(v0ToPoint, normal);
      inFront = dot;

      var normalLengthSquared = normal.LengthSquared;
      //if (normalLengthSquared < Numeric.EpsilonFSquared)
      //{
      //  // Make edge checks.
      //  if (GetCollinearity(point, face.Boundary) == Collinearity.NotCollinear
      //      && GetCollinearity(point, face.Boundary.Next) == Collinearity.NotCollinear)
      //    return Collinearity.NotCollinear;
      //
      //  // Not on the triangle line.
      //  return Collinearity.NotCollinear;
      //}

      if (dot * dot > Numeric.EpsilonF * normalLengthSquared * v0ToPoint.LengthSquared)
      {
        if (dot > 0)
          return Collinearity.NotCollinearInFront;  // Face is visible from point.
        if (dot < 0)
          return Collinearity.NotCollinearBehind;   // Face is not visible from point.
      }

      return Collinearity.Collinear;
    }


    /// <summary>
    /// Determines the collinearity between a point and an edge of a face.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="edge">The edge.</param>
    /// <param name="faceNormal">The face normal (of the face of the edge).</param>
    /// <param name="inFront">
    /// A value that is positive if the point is in front (outside of the face). 
    /// And the value is proportional to the distance of the point from the edge.
    /// </param>
    /// <returns>The collinearity type.</returns>
    internal static Collinearity GetCollinearity(Vector3F point, DcelEdge edge, Vector3F faceNormal, out float inFront)
    {
      Vector3F v0 = edge.Origin.Position;
      Vector3F v1 = edge.Twin.Origin.Position;
      Vector3F segment = v1 - v0;
      float segmentLengthSquared = segment.LengthSquared;

      // See Geometric Tools for Computer Graphics, p. 736 for explanation in 2D.
      // Here we do the same in 3D. Our face normal is normalized.
      Debug.Assert(faceNormal.IsNumericallyNormalized);

      // The needed normal is computed from the face normal.
      // The normal lies in the face plane and points away from the face.
      Vector3F normal = Vector3F.Cross(segment, faceNormal);
      float normalLengthSquared = normal.LengthSquared;

      Vector3F v0ToPoint = point - v0;
      float v0ToPointLengthSquared = v0ToPoint.LengthSquared;

      float dot = Vector3F.Dot(v0ToPoint, normal);
      inFront = dot;
      if (dot * dot > Numeric.EpsilonF * normalLengthSquared * v0ToPointLengthSquared)
      {
        if (dot > 0)
          return Collinearity.NotCollinearInFront;  // Edge is visible from point.
        if (dot < 0)
          return Collinearity.NotCollinearBehind;   // Edge is not visible from point.
      }

      dot = Vector3F.Dot(segment, v0ToPoint);

      if (dot < -Numeric.EpsilonF * segmentLengthSquared)
        return Collinearity.CollinearBefore;
      if (dot > (1 + Numeric.EpsilonF) * segmentLengthSquared)
        return Collinearity.CollinearAfter;

      return Collinearity.CollinearContained;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CustomTiledImage : Image {
    [SerializeField] protected bool m_FlipHorizontal;

    public bool FlipHorizontal {
        get => m_FlipHorizontal;
        set => m_FlipHorizontal = value;
    }
    
    /// <summary>
    /// Update the UI renderer mesh.
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper toFill) {
        Debug.Log($"{gameObject.name} OnPopulateMesh {overrideSprite == null} {type != Type.Tiled}");
        if (overrideSprite == null || type != Type.Tiled) {
            base.OnPopulateMesh(toFill);
            return;
        }

        GenerateTiledSprite(toFill);
    }

    /// <summary>
    /// Generate vertices for a tiled Image.
    /// </summary>
    void GenerateTiledSprite(VertexHelper toFill) {
        Vector4 outer, inner, border;
        Vector2 spriteSize;

        if (overrideSprite != null) {
            outer = UnityEngine.Sprites.DataUtility.GetOuterUV(overrideSprite);
            inner = UnityEngine.Sprites.DataUtility.GetInnerUV(overrideSprite);
            border = overrideSprite.border;
            spriteSize = overrideSprite.rect.size;
        }
        else {
            outer = Vector4.zero;
            inner = Vector4.zero;
            border = Vector4.zero;
            spriteSize = Vector2.one * 100;
        }

        Rect rect = GetPixelAdjustedRect();
        float tileWidth = (spriteSize.x - border.x - border.z) / multipliedPixelsPerUnit;
        float tileHeight = (spriteSize.y - border.y - border.w) / multipliedPixelsPerUnit;

        border = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect);
        Debug.Log($"inner=({inner.x}, {inner.y}, {inner.z}, {inner.w})");

        var uvMin = new Vector2(inner.x, inner.y);
        var uvMax = new Vector2(inner.z, inner.w);

        // Min to max max range for tiled region in coordinates relative to lower left corner.
        float xMin = border.x;
        float xMax = rect.width - border.z;
        float yMin = border.y;
        float yMax = rect.height - border.w;

        toFill.Clear();
        var clipped = uvMax;

        // if either width is zero we cant tile so just assume it was the full width.
        if (tileWidth <= 0)
            tileWidth = xMax - xMin;

        if (tileHeight <= 0)
            tileHeight = yMax - yMin;

        if (overrideSprite != null &&
            (hasBorder || overrideSprite.packed || overrideSprite.texture.wrapMode != TextureWrapMode.Repeat)) {
            Debug.Log($"case 1 overrideSprite?{overrideSprite != null} hasBorder={hasBorder}");
            // Sprite has border, or is not in repeat mode, or cannot be repeated because of packing.
            // We cannot use texture tiling so we will generate a mesh of quads to tile the texture.

            // Evaluate how many vertices we will generate. Limit this number to something sane,
            // especially since meshes can not have more than 65000 vertices.

            long nTilesW = 0;
            long nTilesH = 0;
            if (fillCenter) {
                Debug.Log($"case 1.1 ");
                nTilesW = (long) Math.Ceiling((xMax - xMin) / tileWidth);
                nTilesH = (long) Math.Ceiling((yMax - yMin) / tileHeight);

                double nVertices = 0;
                if (hasBorder) {
                    nVertices = (nTilesW + 2.0) * (nTilesH + 2.0) * 4.0; // 4 vertices per tile
                }
                else {
                    nVertices = nTilesW * nTilesH * 4.0; // 4 vertices per tile
                }
                Debug.Log($"tilesW={nTilesW} tilesH={nTilesH} tileWidth={tileWidth} tileHeight={tileHeight} nVertices={nVertices}");

                if (nVertices > 65000.0) {
                    Debug.LogError(
                        "Too many sprite tiles on Image \"" + name +
                        "\". The tile size will be increased. To remove the limit on the number of tiles, set the Wrap mode to Repeat in the Image Import Settings",
                        this);

                    double maxTiles = 65000.0 / 4.0; // Max number of vertices is 65000; 4 vertices per tile.
                    double imageRatio;
                    if (hasBorder) {
                        imageRatio = (nTilesW + 2.0) / (nTilesH + 2.0);
                    }
                    else {
                        imageRatio = (double) nTilesW / nTilesH;
                    }

                    double targetTilesW = Math.Sqrt(maxTiles / imageRatio);
                    double targetTilesH = targetTilesW * imageRatio;
                    if (hasBorder) {
                        targetTilesW -= 2;
                        targetTilesH -= 2;
                    }

                    nTilesW = (long) Math.Floor(targetTilesW);
                    nTilesH = (long) Math.Floor(targetTilesH);
                    tileWidth = (xMax - xMin) / nTilesW;
                    tileHeight = (yMax - yMin) / nTilesH;
                    Debug.Log($"nVertices > 65k tilesW={nTilesW} tilesH={nTilesH} tileWidth={tileWidth} tileHeight={tileHeight}");
                }
            }
            else {
                Debug.Log($"case 1.2 ");
                if (hasBorder) {
                    Debug.Log($"case 1.2.1 ");
                    // Texture on the border is repeated only in one direction.
                    nTilesW = (long) Math.Ceiling((xMax - xMin) / tileWidth);
                    nTilesH = (long) Math.Ceiling((yMax - yMin) / tileHeight);
                    double nVertices =
                        (nTilesH + nTilesW + 2.0 /*corners*/) * 2.0 /*sides*/ * 4.0 /*vertices per tile*/;
                    if (nVertices > 65000.0) {
                        Debug.LogError(
                            "Too many sprite tiles on Image \"" + name +
                            "\". The tile size will be increased. To remove the limit on the number of tiles, set the Wrap mode to Repeat in the Image Import Settings",
                            this);

                        double maxTiles = 65000.0 / 4.0; // Max number of vertices is 65000; 4 vertices per tile.
                        double imageRatio = (double) nTilesW / nTilesH;
                        double targetTilesW = (maxTiles - 4 /*corners*/) / (2 * (1.0 + imageRatio));
                        double targetTilesH = targetTilesW * imageRatio;

                        nTilesW = (long) Math.Floor(targetTilesW);
                        nTilesH = (long) Math.Floor(targetTilesH);
                        tileWidth = (xMax - xMin) / nTilesW;
                        tileHeight = (yMax - yMin) / nTilesH;
                    }
                }
                else {
                    Debug.Log($"case 1.2.2 ");
                    nTilesH = nTilesW = 0;
                }
            }

            if (fillCenter) {
                Debug.Log($"case 1.2+.1 ");
                // TODO: we could share vertices between quads. If vertex sharing is implemented. update the computation for the number of vertices accordingly.
                for (long j = 0; j < nTilesH; j++) {
                    float y1 = yMin + j * tileHeight;
                    float y2 = yMin + (j + 1) * tileHeight;
                    if (y2 > yMax) {
                        clipped.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                        y2 = yMax;
                    }

                    clipped.x = uvMax.x;
                    for (long i = 0; i < nTilesW; i++) {
                        float x1 = xMin + i * tileWidth;
                        float x2 = xMin + (i + 1) * tileWidth;
                        if (x2 > xMax) {
                            clipped.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                            x2 = xMax;
                        }

                        AddQuad(toFill, new Vector2(x1, y1) + rect.position, new Vector2(x2, y2) + rect.position, color,
                            uvMin, clipped);
                    }
                }
            }

            if (hasBorder) {
                Debug.Log($"case 1.3+.1 ");
                clipped = uvMax;
                for (long j = 0; j < nTilesH; j++) {
                    float y1 = yMin + j * tileHeight;
                    float y2 = yMin + (j + 1) * tileHeight;
                    if (y2 > yMax) {
                        clipped.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                        y2 = yMax;
                    }

                    AddQuad(toFill,
                        new Vector2(0, y1) + rect.position,
                        new Vector2(xMin, y2) + rect.position,
                        color,
                        new Vector2(outer.x, uvMin.y),
                        new Vector2(uvMin.x, clipped.y));
                    AddQuad(toFill,
                        new Vector2(xMax, y1) + rect.position,
                        new Vector2(rect.width, y2) + rect.position,
                        color,
                        new Vector2(uvMax.x, uvMin.y),
                        new Vector2(outer.z, clipped.y));
                }

                // Bottom and top tiled border
                clipped = uvMax;
                for (long i = 0; i < nTilesW; i++) {
                    float x1 = xMin + i * tileWidth;
                    float x2 = xMin + (i + 1) * tileWidth;
                    if (x2 > xMax) {
                        clipped.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                        x2 = xMax;
                    }

                    AddQuad(toFill,
                        new Vector2(x1, 0) + rect.position,
                        new Vector2(x2, yMin) + rect.position,
                        color,
                        new Vector2(uvMin.x, outer.y),
                        new Vector2(clipped.x, uvMin.y));
                    AddQuad(toFill,
                        new Vector2(x1, yMax) + rect.position,
                        new Vector2(x2, rect.height) + rect.position,
                        color,
                        new Vector2(uvMin.x, uvMax.y),
                        new Vector2(clipped.x, outer.w));
                }

                // Corners
                AddQuad(toFill,
                    new Vector2(0, 0) + rect.position,
                    new Vector2(xMin, yMin) + rect.position,
                    color,
                    new Vector2(outer.x, outer.y),
                    new Vector2(uvMin.x, uvMin.y));
                AddQuad(toFill,
                    new Vector2(xMax, 0) + rect.position,
                    new Vector2(rect.width, yMin) + rect.position,
                    color,
                    new Vector2(uvMax.x, outer.y),
                    new Vector2(outer.z, uvMin.y));
                AddQuad(toFill,
                    new Vector2(0, yMax) + rect.position,
                    new Vector2(xMin, rect.height) + rect.position,
                    color,
                    new Vector2(outer.x, uvMax.y),
                    new Vector2(uvMin.x, outer.w));
                AddQuad(toFill,
                    new Vector2(xMax, yMax) + rect.position,
                    new Vector2(rect.width, rect.height) + rect.position,
                    color,
                    new Vector2(uvMax.x, uvMax.y),
                    new Vector2(outer.z, outer.w));
            }
        }
        else {
            // Texture has no border, is in repeat mode and not packed. Use texture tiling.
//            var uvScale = new Vector2((xMax - xMin) / tileWidth, (yMax - yMin) / tileHeight);
            var nTilesW = (long) Math.Ceiling((xMax - xMin) / tileWidth);
            var nTilesH = (long) Math.Ceiling((yMax - yMin) / tileHeight);

            if (fillCenter) {
                
                /* Original logic
                AddQuad(toFill, new Vector2(xMin, yMin) + rect.position, new Vector2(xMax, yMax) + rect.position, color,
                    Vector2.Scale(uvMin, uvScale), Vector2.Scale(uvMax, uvScale));
                */
                
                // New customized logic.
                for (var j = 0; j < nTilesH; j++) {
                    var y1 = yMin + j * tileHeight;
                    var y2 = yMin + (j + 1) * tileHeight;
                    if (y2 > yMax) {
                        clipped.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                        y2 = yMax;
                    }

                    clipped.x = uvMax.x;
                    for (long i = 0; i < nTilesW; i++) {
                        var x1 = xMin + i * tileWidth;
                        var x2 = xMin + (i + 1) * tileWidth;
                        if (x2 > xMax) {
                            clipped.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                            x2 = xMax;
                        }

                        AddQuad(toFill, new Vector2(x1, y1) + rect.position, new Vector2(x2, y2) + rect.position, color,
                            uvMin, clipped, m_FlipHorizontal && j % 2 == 1);
                    }
                }
            }
        }
    }
    
    private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
    {
        Rect originalRect = rectTransform.rect;

        for (int axis = 0; axis <= 1; axis++)
        {
            float borderScaleRatio;

            // The adjusted rect (adjusted for pixel correctness)
            // may be slightly larger than the original rect.
            // Adjust the border to match the adjustedRect to avoid
            // small gaps between borders (case 833201).
            if (originalRect.size[axis] != 0)
            {
                borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                border[axis] *= borderScaleRatio;
                border[axis + 2] *= borderScaleRatio;
            }

            // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
            // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
            float combinedBorders = border[axis] + border[axis + 2];
            if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
            {
                borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                border[axis] *= borderScaleRatio;
                border[axis + 2] *= borderScaleRatio;
            }
        }
        return border;
    }
    
    static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax, bool flipH = false, bool flipV = false)
    {
        if (flipH) {
            var tmp = uvMin.x;
            uvMin.x = uvMax.x;
            uvMax.x = tmp;
        }
        if (flipV) {
            var tmp = uvMin.y;
            uvMin.y = uvMax.y;
            uvMax.y = tmp;
        }
        Debug.Log($"<color=blue>add_quad(pos_min=({posMin.x}, {posMin.y}), posMax=({posMax.x}, {posMax.y}), uvMin=({uvMin.x}, {uvMin.y}), uvMax=({uvMax.x}, {uvMax.y})</color>");
        int startIndex = vertexHelper.currentVertCount;

        Debug.Log($"add_vert(({posMin.x}, {posMin.y}, 0), {color}, ({uvMin.x}, {uvMin.y}))");
        vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
        Debug.Log($"add_vert(({posMin.x}, {posMax.y}, 0), {color}, ({uvMin.x}, {uvMax.y}))");
        vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
        Debug.Log($"add_vert(({posMax.x}, {posMax.y}, 0), {color}, ({uvMax.x}, {uvMax.y}))");
        vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
        Debug.Log($"add_vert(({posMax.x}, {posMin.y}, 0), {color}, ({uvMax.x}, {uvMin.y}))");
        vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

        Debug.Log($"add_triangle({startIndex}, {startIndex + 1}, {startIndex + 2})");
        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        Debug.Log($"add_triangle({startIndex + 2}, {startIndex + 3}, {startIndex})");
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Animations;

public class CompressImage1 : MonoBehaviour {
    [SerializeField]
    Texture2D inImg;
    [SerializeField]
    float tolerance = 0.1f;

    Texture2D textureOutput;
    public Material mat;

    [SerializeField]
    string filename;
    public bool load = false;

    float counter = 0;
    float limit = 1;
    int index = 0;

    QTTexture1 qt;

    private void Start() {
        float temp = Time.realtimeSinceStartup;
        qt = new QTTexture1();
        if (load) qt.LoadTexture(Application.persistentDataPath + "/" + filename + ".qtimg");
        else qt.LoadTexture(inImg, tolerance);
        //Debug.Log("Compression Time: " + (Time.realtimeSinceStartup - temp));
        //temp = Time.realtimeSinceStartup;
        if (!load) qt.SaveToFile(Application.persistentDataPath + "/" + filename + ".qtimg");
        //Debug.Log(Application.dataPath);
        //Debug.Log("Save Time: " + (Time.realtimeSinceStartup - temp));

        textureOutput = qt.ToTexture2D();
        mat.SetTexture(Shader.PropertyToID("_MainTex"), textureOutput);

        //Debug.Log(Application.persistentDataPath);
        Debug.Log("Done Compress");
    }

    //void FixedUpdate() {
    //	//counter += Time.fixedDeltaTime;
    //	if (counter >= limit) {
    //		Debug.Log("Compressing incremental");
    //		counter = 0;
    //		tolerance += 1f;
    //		QTTexture1 qt = Compress();
    //		//qt.SaveToFile(Application.persistentDataPath + "/video" + (index++) + ".qtimg");
    //		textureOutput = qt.ToTexture2D();
    //		mat.SetTexture(Shader.PropertyToID("_MainTex"), textureOutput);
    //	}
    //}
}

class QTTexture1 {
    Quadtree1 data;
    int width;
    int height;
    byte pow;
    QuadtreeLeaves savedData;

    //MyClass mc;

    public QTTexture1() {
        savedData = null;
        data = new Quadtree1();
        width = 0;
        height = 0;
        pow = 0;
    }

    public void LoadTexture(Texture2D tex, float tolerance) {
        width = tex.width;
        height = tex.height;
        data.insertTexture(tex, tolerance, out pow);
    }

    public void LoadTexture(string path) {
        if (File.Exists(path)) {

            byte[] file = File.ReadAllBytes(path);

            using (var memstream = new MemoryStream(file))
            using (var gzipstream = new GZipStream(memstream, CompressionMode.Decompress, true)) {
                byte[] buffer = new byte[4096];
                using (MemoryStream stream = new MemoryStream()) {
                    int count = 0;
                    do {
                        count = gzipstream.Read(buffer, 0, buffer.Length);
                        if (count != 0) {
                            stream.Write(buffer, 0, count);
                        }
                    } while (count != 0);
                    stream.Position = 0;
                    savedData = (QuadtreeLeaves)(new BinaryFormatter().Deserialize(stream));
                    width = savedData.width;
                    height = savedData.height;
                }

                //savedData = (QuadtreeLeaves)(new BinaryFormatter().Deserialize(gzipstream));
                //width = savedData.width;
                //height = savedData.height;

                //mc = (MyClass)(new BinaryFormatter().Deserialize(gzipstream));
                //width = mc.GetWidth();
                //height = mc.GetHeight();
            }

            //BinaryFormatter formatter = new BinaryFormatter();
            //FileStream stream = new FileStream(path, FileMode.Open);

            //savedData = (QuadtreeLeaves)formatter.Deserialize(stream);
            //width = savedData.width;
            //height = savedData.height;

            //stream.Close();
        } else {
            Debug.LogError("QT Image not found");
        }
    }

    public void SaveToFile(string path) {
        if (savedData != null) return;

        using (var memstream = new MemoryStream())
        using (var fstream = new FileStream(path, FileMode.Create))
        using (var gzipstream = new GZipStream(fstream, CompressionMode.Compress, true)) {
            var orderedNodes = data.GetLeaves(pow, width, height).OrderBy(p => (p.pos & 0xFFFF)).ThenBy(p => (p.pos >> 16));
            QuadtreeLeaves leaves = new QuadtreeLeaves(orderedNodes.ToArray(), width, height);
            new BinaryFormatter().Serialize(memstream, leaves);
            byte[] bytes = memstream.ToArray();

            gzipstream.Write(bytes, 0, bytes.Length);

            //Color32[] colors = new Color32[100 * 100];
            //for (int i = 0; i < colors.Length; i++) {
            //    colors[i] = new Color32((byte)(UnityEngine.Random.value * 255), (byte)(UnityEngine.Random.value * 255), (byte)(UnityEngine.Random.value * 255), 255);
            //}

            //mc = new MyClass(colors, 100, 100);
            //new BinaryFormatter().Serialize(gzipstream, mc);
        }
    }

    public Texture2D ToTexture2D() {
        //Texture2D tex = new Texture2D(mc.GetWidth(), mc.GetHeight(), TextureFormat.RGBA32, false);
        //tex.SetPixels32(mc.GetPixels());
        //tex.Apply();
        if (savedData == null) return data.ToTexture2D(width, height, pow);

        //generate from loaded qtimg
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var colors = tex.GetPixels32();

        //initialize with black
        for (int i = 0; i < colors.Length; i++) {
            colors[i] = new Color32(0, 0, 0, 0);
        }

        int smallestSize = (int)Math.Pow(2, savedData.smallestPow);
        int nodeIndex = 0;

        for (int y = 0; y < height; y += smallestSize) {
            int x = 0;
            while (x < width) {
                if (colors[x + y * width].a != 0) {
                    x++;
                    continue;
                }
                if (nodeIndex >= savedData.GetLeafCount()) break;
                var leaf = savedData.GetLeaf(nodeIndex++);
                int size = (int)Math.Pow(2, leaf.pow);
                for (int x2 = 0; x2 < size; x2++) {
                    for (int y2 = 0; y2 < size; y2++) {
                        int colorIndex = (x + x2) + (y + y2) * width;
                        if ((x + x2) >= width || (y + y2) >= height) continue;
                        colors[colorIndex].r = leaf.r;
                        colors[colorIndex].g = leaf.g;
                        colors[colorIndex].b = leaf.b;
                        colors[colorIndex].a = 255;
                    }
                }
            }
        }

        tex.SetPixels32(colors);
        tex.Apply();

        return tex;
    }
}

//[Serializable]
//class MyClass : ISerializable{
//    int width, height;
//    byte[] r, g, b;

//    public MyClass(SerializationInfo info, StreamingContext context) {
//        width = (int)info.GetValue("A", typeof(int));
//        height = (int)info.GetValue("B", typeof(int));

//        r = (byte[])info.GetValue("Red", typeof(byte[]));
//        g = (byte[])info.GetValue("Green", typeof(byte[]));
//        b = (byte[])info.GetValue("Blue", typeof(byte[]));
//    }

//    public void GetObjectData(SerializationInfo info, StreamingContext context) {
//        info.AddValue("A", width);
//        info.AddValue("B", height);

//        info.AddValue("Red", r);
//        info.AddValue("Green", g);
//        info.AddValue("Blue", b);
//    }

//    public MyClass(Color32[] data, int width, int height) {
//        this.width = width;
//        this.height = height;
//        r = new byte[data.Length];
//        g = new byte[data.Length];
//        b = new byte[data.Length];
//        for (int i = 0; i < data.Length; i++) {
//            r[i] = data[i].r;
//            g[i] = data[i].g;
//            b[i] = data[i].b;
//        }
//    }

//    public int GetWidth() {
//        return width;
//    }
//    public int GetHeight() {
//        return height;
//    }

//    public Color32[] GetPixels() {
//        Color32[] pixels = new Color32[r.Length];
//        for (int i = 0; i < r.Length; i++) {
//            pixels[i].r = r[i];
//            pixels[i].g = g[i];
//            pixels[i].b = b[i];
//            pixels[i].a = 255;
//        }
//        return pixels;
//    }
//}

[Serializable]
class QuadtreeLeaves /*: ISerializable*/ {
    public int width, height;
    public byte smallestPow;

    public byte[] pows;
    //public uint[] pos;
    public byte[] r, g, b;

    //public QuadtreeLeaves(SerializationInfo info, StreamingContext context) {
    //    width = (int)info.GetValue("Width", typeof(int));
    //    height = (int)info.GetValue("Height", typeof(int));
    //    smallestPow = (byte)info.GetValue("SmallestPow", typeof(byte));

    //    pows = (byte[])info.GetValue("Pows", typeof(byte[]));
    //    r = (byte[])info.GetValue("Red", typeof(byte[]));
    //    g = (byte[])info.GetValue("Green", typeof(byte[]));
    //    b = (byte[])info.GetValue("Blue", typeof(byte[]));
    //}

    //public void GetObjectData(SerializationInfo info, StreamingContext context) {
    //    info.AddValue("Width", width);
    //    info.AddValue("Height", height);
    //    info.AddValue("SmallestPow", smallestPow);

    //    info.AddValue("Pows", pows);
    //    info.AddValue("Red", r);
    //    info.AddValue("Green", g);
    //    info.AddValue("Blue", b);
    //}

    public QuadtreeLeaves(QuadtreeLeaf[] leaves, int width, int height) {
        if (leaves.Length % 2 == 0)
            pows = new byte[leaves.Length / 2];
        else
            pows = new byte[leaves.Length / 2 + 1];
        //pos = new uint[leaves.Length];

        r = new byte[leaves.Length];
        g = new byte[leaves.Length];
        b = new byte[leaves.Length];

        //denotes whether the next pow is stored in the high 4 bits or the low four bits of a byte
        bool high = true;
        for (int i = 0; i < leaves.Length; i++) {
            if (high) {
                pows[i / 2] = (byte)(leaves[i].pow << 4);
            } else {
                pows[i / 2] += leaves[i].pow;
            }
            //toggle
            high = !high;
            smallestPow = (byte)(Convert.ToByte(leaves[i].pow < smallestPow) * leaves[i].pow + Convert.ToByte(leaves[i].pow >= smallestPow) * smallestPow);
            //pos[i] = leaves[i].pos;
            r[i] = leaves[i].r;
            g[i] = leaves[i].g;
            b[i] = leaves[i].b;
        }

        this.width = width;
        this.height = height;
    }

    public QuadtreeLeaf GetLeaf(int index) {
        byte pow;
        if (index % 2 == 0)
            pow = (byte)(pows[index / 2] >> 4);
        else
            pow = (byte)(pows[index / 2] & 0xF);
        return new QuadtreeLeaf(pow, 0, r[index], g[index], b[index]);
    }

    public int GetLeafCount() {
        return r.Length;
    }
}

class QuadtreeLeaf {
    public byte pow;
    public uint pos;
    public byte r, g, b;

    public QuadtreeLeaf(byte pow, uint pos, byte r, byte g, byte b) {
        this.pow = pow;
        this.pos = pos;
        this.r = r;
        this.g = g;
        this.b = b;
    }
}

class Quadtree1 {
    Quadtree1[] children;
    byte red;
    byte green;
    byte blue;

    public Quadtree1() {
        red = green = blue = 0;
    }

    public void insertTexture(Texture2D tex, float tolerance, out byte power) {
        int w = tex.width;
        int h = tex.height;
        int lw = (int)Mathf.Ceil(Mathf.Log(w, 2));
        int lh = (int)Mathf.Ceil(Mathf.Log(h, 2));
        if (lw < lh) {
            lw = lh;
        }

        int size = (int)Mathf.Pow(2, lw);

        byte pow = power = (byte)lw;

        if (size == w && size == h) {
            insertTextureRec(tex.GetPixels32(), size, 0, 0, pow, tolerance);
        } else {
            Texture2D newTex = new Texture2D(size, size, tex.format, false);
            var blackArr = newTex.GetPixels32();
            //initialize with black
            for (int i = 0; i < blackArr.Length; i++) {
                blackArr[i] = new Color32(0, 0, 0, 0);
            }
            newTex.SetPixels32(blackArr);
            newTex.SetPixels32(0, 0, w, h, tex.GetPixels32());
            newTex.Apply();
            insertTextureRec(newTex.GetPixels32(), size, 0, 0, pow, tolerance);
        }
    }

    bool insertTextureRec(Color32[] tex, int texSize, int x, int y, byte pow, float tolerance) {
        int size = (int)Mathf.Pow(2, pow);
        if (size == 1) {
            Color c = tex[x + y * texSize];
            red = (byte)(c.r * 255);
            green = (byte)(c.g * 255);
            blue = (byte)(c.b * 255);
            return Convert.ToBoolean(c.a);
        }


        TrySubdivide();

        if (!children[0].insertTextureRec(tex, texSize, x, y, (byte)(pow - 1), tolerance))
            children[0] = null;
        if (!children[1].insertTextureRec(tex, texSize, x + size / 2, y, (byte)(pow - 1), tolerance))
            children[1] = null;
        if (!children[2].insertTextureRec(tex, texSize, x, y + size / 2, (byte)(pow - 1), tolerance))
            children[2] = null;
        if (!children[3].insertTextureRec(tex, texSize, x + size / 2, y + size / 2, (byte)(pow - 1), tolerance))
            children[3] = null;

        //only void if all children are void
        bool isVoid = children[0] == null && children[1] == null && children[2] == null && children[3] == null;
        if (isVoid) return false;


        //apply average child color to current node
        Color32[] colors = new Color32[] {   children[0] == null ? new Color32(0,0,0,0) : children[0].GetColor(),
                                        children[1] == null ? new Color32(0,0,0,0) : children[1].GetColor(),
                                        children[2] == null ? new Color32(0,0,0,0) : children[2].GetColor(),
                                        children[3] == null ? new Color32(0,0,0,0) : children[3].GetColor() };
        red = (byte)((colors[0].r + colors[1].r + colors[2].r + colors[3].r) / 4);
        green = (byte)((colors[0].g + colors[1].g + colors[2].g + colors[3].g) / 4);
        blue = (byte)((colors[0].b + colors[1].b + colors[2].b + colors[3].b) / 4);

        //determine color distance of each child color to the average color
        int dist0 = (colors[0].r - red) * (colors[0].r - red) +
                    (colors[0].g - green) * (colors[0].g - green) +
                    (colors[0].b - blue) * (colors[0].b - blue);
        int dist1 = (colors[1].r - red) * (colors[1].r - red) +
                    (colors[1].g - green) * (colors[1].g - green) +
                    (colors[1].b - blue) * (colors[1].b - blue);
        int dist2 = (colors[2].r - red) * (colors[2].r - red) +
                    (colors[2].g - green) * (colors[2].g - green) +
                    (colors[2].b - blue) * (colors[2].b - blue);
        int dist3 = (colors[3].r - red) * (colors[3].r - red) +
                    (colors[3].g - green) * (colors[3].g - green) +
                    (colors[3].b - blue) * (colors[3].b - blue);
        float t2 = tolerance * tolerance;
        //if maximum tolerance distance is not exceeded, children are disposed, making the parent the end node
        if (dist0 < t2 && dist1 < t2 && dist2 < t2 && dist3 < t2) {
            children = null;
        }
        return true;
    }

    void TrySubdivide() {
        if (children != null) return;
        children = new Quadtree1[4];
        for (int i = 0; i < children.Length; i++) {
            children[i] = new Quadtree1();
        }
    }

    public Texture2D ToTexture2D(int width, int height, byte pow) {
        var tex = new Texture2D(width, height);
        tex.SetPixels(ToColorArrayRec(new Color[width * height], width, height, 0, 0, pow));
        tex.Apply();
        return tex;
    }

    Color[] ToColorArrayRec(Color[] colors, int width, int height, int x, int y, int pow) {
        int size = (int)Mathf.Pow(2, pow);
        if (!hasChildren()) {
            for (int i = x; i < x + size && i < width; i++) {
                for (int j = y; j < y + size && j < height; j++) {
                    colors[i + j * width] = new Color(red / 255.0f, green / 255.0f, blue / 255.0f, 1.0f);
                }
            }
        } else {
            children[0]?.ToColorArrayRec(colors, width, height, x, y, pow - 1);
            children[1]?.ToColorArrayRec(colors, width, height, x + size / 2, y, pow - 1);
            children[2]?.ToColorArrayRec(colors, width, height, x, y + size / 2, pow - 1);
            children[3]?.ToColorArrayRec(colors, width, height, x + size / 2, y + size / 2, pow - 1);
        }
        return colors;
    }

    public List<QuadtreeLeaf> GetLeaves(byte pow, int width, int height) {
        return GetLeavesRec(pow, 0, width, height);
    }

    public List<QuadtreeLeaf> GetLeavesRec(byte pow, uint pos, int width, int height) {
        uint size = (uint)Mathf.Pow(2, pow);
        uint x = (pos >> 16);
        uint y = (pos & 0xFFFF);
        if (x >= width || y >= height) return null;
        List<QuadtreeLeaf> leaves = new List<QuadtreeLeaf>();
        if (!hasChildren()) {
            leaves.Add(new QuadtreeLeaf(pow, pos, red, green, blue));
        } else {
            if (children[0] != null) {
                var list = children[0].GetLeavesRec((byte)(pow - 1), (x << 16) + y, width, height);
                if (list != null)
                    leaves.AddRange(list);
            }
            if (children[1] != null) {
                var list = children[1].GetLeavesRec((byte)(pow - 1), ((x + size / 2) << 16) + y, width, height);
                if (list != null)
                    leaves.AddRange(list);
            }
            if (children[2] != null) {
                var list = children[2].GetLeavesRec((byte)(pow - 1), (x << 16) + y + size / 2, width, height);
                if (list != null)
                    leaves.AddRange(list);
            }
            if (children[3] != null) {
                var list = children[3].GetLeavesRec((byte)(pow - 1), ((x + size / 2) << 16) + y + size / 2, width, height);
                if (list != null)
                    leaves.AddRange(list);
            }
        }
        return leaves;
    }

    public bool hasChildren() {
        return children != null;
    }

    public Color32 GetColor() {
        return new Color32(red, green, blue, 1);
    }
}
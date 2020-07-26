using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Animations;

public class CompressImage1 : MonoBehaviour {
	[SerializeField]
	Texture2D inImg;
	[SerializeField]
	float tolerance = 0.1f;
	[SerializeField]
	string filename;
	Texture2D textureOutput;
	public Material mat;

	float counter = 0;
	float limit = 1;
	int index = 0;

	private void Start() {
		//float temp = Time.realtimeSinceStartup;
		QTTexture1 qt = Compress();
		//Debug.Log("Compression Time: " + (Time.realtimeSinceStartup - temp));
		//temp = Time.realtimeSinceStartup;
		//qt.SaveToFile(Application.persistentDataPath + "/" + filename + ".qtimg");
		//Debug.Log("Save Time: " + (Time.realtimeSinceStartup - temp));
		textureOutput = qt.ToTexture2D();
		mat.SetTexture(Shader.PropertyToID("_MainTex"), textureOutput);
		//Debug.Log(Application.persistentDataPath);
	}

	void FixedUpdate() {
		counter += Time.fixedDeltaTime;
		if (counter >= limit) {
			Debug.Log("Compressing incremental");
			counter = 0;
			tolerance += 1f;
			QTTexture1 qt = Compress();
			//qt.SaveToFile(Application.persistentDataPath + "/video" + (index++) + ".qtimg");
			textureOutput = qt.ToTexture2D();
			mat.SetTexture(Shader.PropertyToID("_MainTex"), textureOutput);
		}
	}

	QTTexture1 Compress() {
		QTTexture1 qt = new QTTexture1();
		qt.loadTexture(inImg, tolerance);

		return qt;
	}
}

class QTTexture1 {
	Quadtree1 data;
	int width;
	int height;
	byte pow;

	public QTTexture1() {
		data = new Quadtree1();
		width = 0;
		height = 0;
		pow = 0;
	}

	public void loadTexture(Texture2D tex, float tolerance) {
		width = tex.width;
		height = tex.height;
		data.insertTexture(tex, tolerance, out pow);
	}

	public static QuadtreeLeaves loadTexture(string path) {
		if (File.Exists(path)) {
			BinaryFormatter formatter = new BinaryFormatter();
			FileStream stream = new FileStream(path, FileMode.Open);

			QuadtreeLeaves qtl = formatter.Deserialize(stream) as QuadtreeLeaves;

			stream.Close();
			return qtl;
		} else {
			Debug.LogError("QT Image not found");
			return null;
		}
	}

	public void SaveToFile(string path) {
		BinaryFormatter formatter = new BinaryFormatter();
		FileStream stream = new FileStream(path, FileMode.Create);
		QuadtreeLeaves leaves = new QuadtreeLeaves(data.GetLeaves(pow), width, height, pow);
		formatter.Serialize(stream, leaves);
		stream.Close();

	}

	public Texture2D ToTexture2D() {
		return data.ToTexture2D(width, height, pow);
	}
}

[Serializable]
class QuadtreeLeaves {
	int width, height;
	byte pow;

	public byte[] pows;
	public uint[] pos;
	public byte[] r, g, b;

	public QuadtreeLeaves(QuadtreeLeaf[] leaves, int width, int height, byte pow) {
		pows = new byte[leaves.Length];
		pos = new uint[leaves.Length];
		r = new byte[leaves.Length];
		g = new byte[leaves.Length];
		b = new byte[leaves.Length];

		for (int i = 0; i < leaves.Length; i++) {
			pows[i] = leaves[i].pow;
			pos[i] = leaves[i].pos;
			r[i] = leaves[i].r;
			g[i] = leaves[i].g;
			b[i] = leaves[i].b;
		}

		this.width = width;
		this.height = height;
		this.pow = pow;
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
			insertTextureRec(tex, 0, 0, pow, tolerance);
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
			insertTextureRec(newTex, 0, 0, pow, tolerance);
		}
	}

	bool insertTextureRec(Texture2D tex, int x, int y, byte pow, float tolerance) {
		int size = (int)Mathf.Pow(2, pow);
		if (size == 1) {
			Color c = tex.GetPixel(x, y);
			red = (byte)(c.r * 255);
			green = (byte)(c.g * 255);
			blue = (byte)(c.b * 255);
			return Convert.ToBoolean(c.a);
		}


		TrySubdivide();

		if (!children[0].insertTextureRec(tex, x, y, (byte)(pow - 1), tolerance))
			children[0] = null;
		if (!children[1].insertTextureRec(tex, x + size / 2, y, (byte)(pow - 1), tolerance))
			children[1] = null;
		if (!children[2].insertTextureRec(tex, x, y + size / 2, (byte)(pow - 1), tolerance))
			children[2] = null;
		if (!children[3].insertTextureRec(tex, x + size / 2, y + size / 2, (byte)(pow - 1), tolerance))
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
		if (dist0 <= t2 && dist1 <= t2 && dist2 <= t2 && dist3 <= t2) {
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

	public QuadtreeLeaf[] GetLeaves(byte pow) {
		return GetLeavesRec(pow, 0, 1).ToArray();
	}

	public List<QuadtreeLeaf> GetLeavesRec(byte pow, uint pos, int depth) {
		int size = (int)Mathf.Pow(2, pow);
		uint offset = (uint)Mathf.Pow(2, 15 - depth);//Mathf.Pow(2, 15) / Mathf.Pow(2, depth);
		List<QuadtreeLeaf> leaves = new List<QuadtreeLeaf>();
		if (!hasChildren()) {
			leaves.Add(new QuadtreeLeaf(pow, pos, red, green, blue));
		} else {
			if (children[0] != null) {
				uint x = (pos >> 8) - offset;
				uint y = (pos % 1 << 8) - offset;
				leaves.AddRange(children[0].GetLeavesRec((byte)(pow - 1), (x << 8) + y, depth + 1));
			}
			if (children[1] != null) {
				uint x = (pos >> 8) + offset;
				uint y = (pos % 1 << 8) - offset;
				leaves.AddRange(children[1].GetLeavesRec((byte)(pow - 1), (x << 8) + y, depth + 1));
			}
			if (children[2] != null) {
				uint x = (pos >> 8) - offset;
				uint y = (pos % 1 << 8) + offset;
				leaves.AddRange(children[2].GetLeavesRec((byte)(pow - 1), (x << 8) + y, depth + 1));
			}
			if (children[3] != null) {
				uint x = (pos >> 8) + offset;
				uint y = (pos % 1 << 8) + offset;
				leaves.AddRange(children[3].GetLeavesRec((byte)(pow - 1), (x << 8) + y, depth + 1));
			}
		}
		return leaves;
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

	public bool hasChildren() {
		return children != null;
	}

	public Color32 GetColor() {
		return new Color32(red, green, blue, 1);
	}
}
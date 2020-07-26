using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Animations;

public class CompressImage : MonoBehaviour {
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


	private void Start() {
		float temp = Time.realtimeSinceStartup;
		QTTexture qt = Compress();
		Debug.Log("Compression Time: " + (Time.realtimeSinceStartup - temp));
		temp = Time.realtimeSinceStartup;
		textureOutput = qt.ToTexture2D();
		qt.SaveToFile(Application.persistentDataPath + "/" + filename + ".qtimg");
		Debug.Log("Save Time: " + (Time.realtimeSinceStartup - temp));
		mat.SetTexture(Shader.PropertyToID("_MainTex"), textureOutput);
		Debug.Log(Application.persistentDataPath);
	}

	void FixedUpdate() {
		//counter += Time.fixedDeltaTime;
		if (counter >= limit) {
			counter = 0;
			tolerance += 1;
			QTTexture qt = Compress();
			textureOutput = qt.ToTexture2D();
			mat.SetTexture(Shader.PropertyToID("_MainTex"), textureOutput);
		}
	}

	QTTexture Compress() {
		QTTexture qt = new QTTexture();
		qt.loadTexture(inImg, tolerance);

		return qt;
	}
}

[System.Serializable]
class QTTexture {
	Quadtree data;
	int width;
	int height;
	byte pow;

	public QTTexture() {
		data = new Quadtree();
		width = 0;
		height = 0;
		pow = 0;
	}

	public void loadTexture(Texture2D tex, float tolerance) {
		width = tex.width;
		height = tex.height;
		data.insertTexture(tex, tolerance, out pow);
	}

	public static QTTexture loadTexture(string path) {
		if (File.Exists(path)) {
			BinaryFormatter formatter = new BinaryFormatter();
			FileStream stream = new FileStream(path, FileMode.Open);

			QTTexture qtt = formatter.Deserialize(stream) as QTTexture;

			stream.Close();
			return qtt;
		} else {
			Debug.LogError("QT Image not found");
			return null;
		}
	}

	public void SaveToFile(string path) {
		BinaryFormatter formatter = new BinaryFormatter();
		FileStream stream = new FileStream(path, FileMode.Create);
		formatter.Serialize(stream, this);
		stream.Close();

	}

	public Texture2D ToTexture2D() {
		return data.ToTexture2D(width, height, pow);
	}
}



[System.Serializable]
class Quadtree {
	Quadtree[] children;
	byte red;
	byte green;
	byte blue;

	public Quadtree() {
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
			//initialize with black
			for (int i = 0; i < newTex.width; i++) {
				for (int j = 0; j < newTex.height; j++) {
					newTex.SetPixel(i, j, new Color(0, 0, 0, 0));
				}
			}
			newTex.SetPixels(0, 0, w, h, tex.GetPixels());
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

		//if maximum tolerance distance is not exceeded, children are disposed, making the parent the end node
		if (Mathf.Sqrt(dist0) <= tolerance && Mathf.Sqrt(dist1) <= tolerance && Mathf.Sqrt(dist2) <= tolerance && Mathf.Sqrt(dist3) <= tolerance) {
			children = null;
		}
		return true;
	}

	void TrySubdivide() {
		if (children != null) return;
		children = new Quadtree[4];
		for (int i = 0; i < children.Length; i++) {
			children[i] = new Quadtree();
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

	public bool hasChildren() {
		return children != null;
	}

	public Color32 GetColor() {
		return new Color32(red, green, blue, 1);
	}
}
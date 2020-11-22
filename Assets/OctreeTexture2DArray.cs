using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeTexture2DArray {

	OctreeTexture2DArray[] children;
	byte red, green, blue;

	public void InsertTextureArr(Texture2D[] textures, float tolerance) {
		if (textures.Length == 0) return;

		int w = textures[0].width;
		int h = textures[0].height;
		int d = textures.Length;

		int lw = (int)Mathf.Ceil(Mathf.Log(w, 2));
		int lh = (int)Mathf.Ceil(Mathf.Log(h, 2));
		int ld = (int)Mathf.Ceil(Mathf.Log(d, 2));

		byte pow = (byte)Mathf.Max(Mathf.Max(lw, lh), ld);

		int size = (int)Mathf.Pow(2, pow);

		//convert texture array to cube format to ensure compatibility with octree
		if(size == w && size == h && size == d) {
			//convert to array of colors
			List<Color32> colors = new List<Color32>();
			foreach (var item in textures) {
				colors.AddRange(item.GetPixels32());
			}

			insertTextureArrRec(colors.ToArray(), size, 0, 0, 0, pow, tolerance);
		} else if (size == w && size == h) {
			//Create transparent texture to fill the rest of the array
			Texture2D newTex = new Texture2D(size, size, textures[0].format, false);
			var blackArr = newTex.GetPixels32();
			for (int i = 0; i < blackArr.Length; i++) {
				blackArr[i] = new Color32(0, 0, 0, 0);
			}
			newTex.SetPixels32(blackArr);
			newTex.Apply();

			Texture2D[] newArr = new Texture2D[size];
			Array.Copy(textures, newArr, textures.Length);
			for (int i = textures.Length; i < size; i++) {
				newArr[i] = newTex;
			}

			//convert to array of colors
			List<Color32> colors = new List<Color32>();
			foreach (var item in newArr) {
				colors.AddRange(item.GetPixels32());
			}

			insertTextureArrRec(colors.ToArray(), size, 0, 0, 0, pow, tolerance);
		} else {
			//Create transparent texture to fill the rest of the array
			Texture2D newTex = new Texture2D(size, size, textures[0].format, false);
			var blackArr = newTex.GetPixels32();
			for (int i = 0; i < blackArr.Length; i++) {
				blackArr[i] = new Color32(0, 0, 0, 0);
			}
			newTex.SetPixels32(blackArr);
			newTex.Apply();
			for (int i = 0; i < textures.Length; i++) {
				Texture2D square = new Texture2D(size, size, textures[i].format, false);
				square.SetPixels32(blackArr);
				square.SetPixels32(0, 0, w, h, textures[i].GetPixels32());
				square.Apply();
				textures[i] = square;
			}

			Texture2D[] newArr = new Texture2D[size];
			Array.Copy(textures, newArr, textures.Length);
			for (int i = textures.Length; i < size; i++) {
				newArr[i] = newTex;
			}

			//convert to array of colors
			List<Color32> colors = new List<Color32>();
			foreach (var item in newArr) {
				colors.AddRange(item.GetPixels32());
			}

			insertTextureArrRec(colors.ToArray(), size, 0, 0, 0, pow, tolerance);
		}
	}

	bool insertTextureArrRec(Color32[] colors, int texSize, int x, int y, int z, byte pow, float tolerance) {
		int size = (int)Mathf.Pow(2, pow);
		if (size == 1) {
			Color32 c = colors[x + y * texSize + z * texSize * texSize];
			red = c.r;
			green = c.g;
			blue = c.b;
			//transparent means node is empty
			return Convert.ToBoolean(c.a);
		}

		TrySubdivide();

		if (!children[0].insertTextureArrRec(colors, texSize, x, y, z, (byte)(pow - 1), tolerance))
			children[0] = null;
		if (!children[1].insertTextureArrRec(colors, texSize, x + size / 2, y, z, (byte)(pow - 1), tolerance))
			children[1] = null;
		if (!children[2].insertTextureArrRec(colors, texSize, x, y + size / 2, z, (byte)(pow - 1), tolerance))
			children[2] = null;
		if (!children[3].insertTextureArrRec(colors, texSize, x + size / 2, y + size / 2, z, (byte)(pow - 1), tolerance))
			children[3] = null;

		if (!children[4].insertTextureArrRec(colors, texSize, x, y, z + size / 2, (byte)(pow - 1), tolerance))
			children[4] = null;
		if (!children[5].insertTextureArrRec(colors, texSize, x + size / 2, y, z + size / 2, (byte)(pow - 1), tolerance))
			children[5] = null;
		if (!children[6].insertTextureArrRec(colors, texSize, x, y + size / 2, z + size / 2, (byte)(pow - 1), tolerance))
			children[6] = null;
		if (!children[7].insertTextureArrRec(colors, texSize, x + size / 2, y + size / 2, z + size / 2, (byte)(pow - 1), tolerance))
			children[7] = null;

		//only void if all children are void
		bool isVoid = children[0] == null && children[1] == null && children[2] == null && children[3] == null && children[4] == null && children[5] == null && children[6] == null && children[7] == null;
		if (isVoid) return false;

		//apply average child color to current node
		Color32[] childColors = new Color32[] {   children[0] == null ? new Color32(0,0,0,0) : children[0].GetColor(),
										children[1] == null ? new Color32(0,0,0,0) : children[1].GetColor(),
										children[2] == null ? new Color32(0,0,0,0) : children[2].GetColor(),
										children[3] == null ? new Color32(0,0,0,0) : children[3].GetColor(),
										children[4] == null ? new Color32(0,0,0,0) : children[4].GetColor(),
										children[5] == null ? new Color32(0,0,0,0) : children[5].GetColor(),
										children[6] == null ? new Color32(0,0,0,0) : children[6].GetColor(),
										children[7] == null ? new Color32(0,0,0,0) : children[7].GetColor()};
		red = (byte)((colors[0].r + colors[1].r + colors[2].r + colors[3].r + colors[4].r + colors[5].r + colors[6].r + colors[7].r) / 8);
		green = (byte)((colors[0].g + colors[1].g + colors[2].g + colors[3].g + colors[4].g + colors[5].g + colors[6].g + colors[7].g) / 8);
		blue = (byte)((colors[0].b + colors[1].b + colors[2].b + colors[3].b + colors[4].b + colors[5].b + colors[6].b + colors[7].b) / 8);

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

		int dist4 = (colors[4].r - red) * (colors[4].r - red) +
					(colors[4].g - green) * (colors[4].g - green) +
					(colors[4].b - blue) * (colors[4].b - blue);
		int dist5 = (colors[5].r - red) * (colors[5].r - red) +
					(colors[5].g - green) * (colors[5].g - green) +
					(colors[5].b - blue) * (colors[5].b - blue);
		int dist6 = (colors[6].r - red) * (colors[6].r - red) +
					(colors[6].g - green) * (colors[6].g - green) +
					(colors[6].b - blue) * (colors[6].b - blue);
		int dist7 = (colors[7].r - red) * (colors[7].r - red) +
					(colors[7].g - green) * (colors[7].g - green) +
					(colors[7].b - blue) * (colors[7].b - blue);
		float t2 = tolerance * tolerance;
		//if maximum tolerance distance is not exceeded, children are disposed, making the parent the end node
		if (dist0 < t2 && dist1 < t2 && dist2 < t2 && dist3 < t2 && dist4 < t2 && dist5 < t2 && dist6 < t2 && dist7 < t2) {
			children = null;
		}

		return true;
	}

	void TrySubdivide() {
		if (children != null) return;
		children = new OctreeTexture2DArray[8];
		for (int i = 0; i < children.Length; i++) {
			children[i] = new OctreeTexture2DArray();
		}
	}

	public Color32 GetColor() {
		return new Color32(red, green, blue, 1);
	}

	public Color[] GetFrame(int frame) {
		return new Color[1];
	}

	public OctreeTexture2DArray() {
		red = green = blue = 0;
	}
}

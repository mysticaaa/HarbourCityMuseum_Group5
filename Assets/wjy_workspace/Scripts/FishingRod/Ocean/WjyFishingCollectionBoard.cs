using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Wjy.Museum
{
    public sealed class WjyFishingCollectionBoard : MonoBehaviour
    {
        private const float FaceTilt = 30f;

        // Centres in the 1440 x 860 catch preview, measured from the top-left.
        // Each crop stays inside its own photograph, clear of the caption and adjacent catches.
        private static readonly (string name, Vector2 centre, float height)[] Photos =
        {
            ("Fish", new Vector2(181, 203), 182),
            ("Sea turtle", new Vector2(536, 155), 148),
            ("Fiddler crab", new Vector2(899, 206), 180),
            ("Glass bottle", new Vector2(1260, 190), 162),
            ("Spiny lobster", new Vector2(175, 602), 158),
            ("Spotted seal", new Vector2(547, 590), 99),
            ("Humpback whale", new Vector2(902, 554), 139)
        };

        private readonly Text[] questions = new Text[Photos.Length];
        private readonly Text[] names = new Text[Photos.Length];
        private readonly RawImage[] thumbnails = new RawImage[Photos.Length];
        private Text progress;

        internal static WjyFishingCollectionBoard Build(Transform raft, Camera view, IReadOnlyCollection<WjyCatchKind> discovered)
        {
            var board = new GameObject("Fishing Collection", typeof(RectTransform), typeof(Canvas),
                typeof(WjyFishingCollectionBoard)).GetComponent<WjyFishingCollectionBoard>();
            var root = (RectTransform)board.transform;
            root.gameObject.layer = LayerMask.NameToLayer("UI");
            root.SetParent(raft, false);
            root.sizeDelta = new Vector2(632, 366);
            root.localPosition = new Vector3(1.20f, .72f, .70f);
            var outward = new Vector3(root.localPosition.x, 0f, root.localPosition.z);
            // A world-space Canvas is read from its local -Z side.
            root.localRotation = Quaternion.LookRotation(outward) * Quaternion.Euler(FaceTilt, 0f, 0f);
            root.localScale = Vector3.one * .00112f;

            // This is a passive sign on the starboard edge, clear of the rod socket and the open bow.
            var canvas = board.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = view;
            board.BuildFace(root);
            board.SetDiscovered(discovered);
            return board;
        }

        private void BuildFace(RectTransform root)
        {
            Color32 timber = new Color32(102, 67, 42, 255);
            // Hang each upright from the tilted rim down to the raft deck (local Y = 0).
            float jointY = root.rect.yMin + 5f;
            float postHeight = root.localPosition.y / root.localScale.y + jointY * Mathf.Cos(FaceTilt * Mathf.Deg2Rad);
            for (int side = -1; side <= 1; side += 2)
            {
                var post = Plate(root, side < 0 ? "Left post" : "Right post", new Vector2(side * 189, jointY),
                    new Vector2(23, postHeight), timber);
                post.pivot = new Vector2(.5f, 1f);
                post.localRotation = Quaternion.Euler(-FaceTilt, 0f, 0f);
            }
            Plate(root, "Timber rim", Vector2.zero, root.sizeDelta, timber);
            Plate(root, "Face", Vector2.zero, root.sizeDelta - new Vector2(12, 12), new Color32(55, 40, 32, 255));
            progress = WjyOceanFishingUI.AddText(root, "Progress", "", new Vector2(0, 133), new Vector2(580, 67), 39);
            progress.fontStyle = FontStyle.Bold;
            progress.color = new Color32(255, 235, 196, 255);

            var photoSheet = Resources.Load<Material>("WjyFishingCollectionPhotos");
            var photos = photoSheet ? photoSheet.mainTexture : null;
            if (!photos) Debug.LogWarning("Fishing Collection photos are missing; showing catch names.", this);
            for (int i = 0; i < Photos.Length; i++)
            {
                bool topRow = i < 4;
                float x = topRow ? (i - 1.5f) * 143 : (i - 5f) * 143;
                var tile = Plate(root, ((WjyCatchKind)i).ToString(), new Vector2(x, topRow ? 31 : -100),
                    new Vector2(136, 122), new Color32(30, 43, 50, 255));
                questions[i] = WjyOceanFishingUI.AddText(tile, "Undiscovered", "?", Vector2.zero, tile.sizeDelta, 52);
                questions[i].color = new Color32(227, 210, 173, 255);

                var photo = new GameObject("Photo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                photo.gameObject.layer = root.gameObject.layer;
                photo.rectTransform.SetParent(tile, false);
                photo.rectTransform.anchoredPosition = new Vector2(0, 15);
                photo.rectTransform.sizeDelta = new Vector2(114, 80);
                photo.raycastTarget = false;
                photo.texture = photos;
                var crop = Photos[i];
                float width = crop.height * photo.rectTransform.rect.width / photo.rectTransform.rect.height;
                photo.uvRect = new Rect((crop.centre.x - width / 2f) / 1440f,
                    1f - (crop.centre.y + crop.height / 2f) / 860f, width / 1440f, crop.height / 860f);
                thumbnails[i] = photo;
                names[i] = WjyOceanFishingUI.AddText(tile, "Name", crop.name, new Vector2(0, photos ? -41 : 0),
                    new Vector2(130, 28), 21);
            }
        }

        internal void SetDiscovered(IReadOnlyCollection<WjyCatchKind> catches)
        {
            int count = 0;
            for (int i = 0; i < Photos.Length; i++)
            {
                bool found = catches.Contains((WjyCatchKind)i);
                questions[i].gameObject.SetActive(!found);
                thumbnails[i].gameObject.SetActive(found && thumbnails[i].texture);
                names[i].gameObject.SetActive(found);
                if (found) count++;
            }
            progress.text = $"Fishing Collection · {count}/{Photos.Length}";
        }

        private static RectTransform Plate(RectTransform parent, string name, Vector2 centre, Vector2 size, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.gameObject.layer = parent.gameObject.layer;
            image.raycastTarget = false;
            image.rectTransform.SetParent(parent, false);
            image.rectTransform.anchoredPosition = centre;
            image.rectTransform.sizeDelta = size;
            image.color = color;
            return image.rectTransform;
        }
    }
}

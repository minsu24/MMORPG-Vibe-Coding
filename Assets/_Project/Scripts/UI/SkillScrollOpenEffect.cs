using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed class SkillScrollOpenEffect : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport, leftRoller, rightRoller;
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private CanvasGroup[] contentGroups;
        [SerializeField] private Image background;
        [SerializeField, Min(.01f)] private float duration = .65f;
        private Sequence sequence;
        private float[] alphas;
        private bool[] interactable, raycasts;
        private Color backgroundColor;
        private float progress, paperWidth, leftEnd, rightEnd, leftStart, rightStart;
        private bool captured;
        private Sprite sourceSprite, paperSprite, leftSprite, rightSprite;
        public bool IsAnimating => sequence != null && sequence.IsActive();

        public void Configure(RectTransform mask, RectTransform left, RectTransform right,
            CanvasGroup art, CanvasGroup[] content, Image existingBackground)
        {
            viewport=mask; leftRoller=left; rightRoller=right;
            overlay=art; contentGroups=content; background=existingBackground;
        }

        private void OnEnable()
        {
            if(!viewport || !overlay || contentGroups==null || !background || !background.sprite) return;
            Canvas.ForceUpdateCanvases();
            Capture();
            PrepareArtwork();
            overlay.gameObject.SetActive(true);
            overlay.alpha=1;
            overlay.blocksRaycasts=true;
            foreach(var group in contentGroups) {
                group.alpha=0; group.interactable=false; group.blocksRaycasts=false;
            }
            var color=backgroundColor; color.a=0; background.color=color;
            ApplyProgress(0);
            sequence=DOTween.Sequence().SetUpdate(true);
            sequence.Append(DOTween.To(()=>progress,ApplyProgress,1,duration).SetEase(Ease.OutCubic));
            // Both representations use exactly the same pixels and geometry.
            // Swap at full extension instead of crossfading mismatched scroll art.
            sequence.AppendCallback(()=> {
                background.color=backgroundColor;
                overlay.gameObject.SetActive(false);
            });
            sequence.AppendInterval(.16f);
            for(int i=0;i<contentGroups.Length;i++) sequence.Join(contentGroups[i].DOFade(alphas[i],.16f));
            sequence.OnComplete(()=>{ sequence=null; Restore(); });
        }

        private void PrepareArtwork()
        {
            var source=background.sprite;
            var rect=source.rect;
            int leftCut=Mathf.RoundToInt(rect.width*190f/1672f);
            int rightCut=Mathf.RoundToInt(rect.width*1480f/1672f);
            if(sourceSprite!=source) {
                ReleaseSprites();
                sourceSprite=source;
                leftSprite=Slice(source,new Rect(rect.x,rect.y,leftCut,rect.height));
                paperSprite=Slice(source,new Rect(rect.x+leftCut,rect.y,rightCut-leftCut,rect.height));
                rightSprite=Slice(source,new Rect(rect.x+rightCut,rect.y,rect.width-rightCut,rect.height));
            }
            var draw=background.GetPixelAdjustedRect();
            var size=draw.size;
            if(background.preserveAspect) {
                float scale=Mathf.Min(size.x/rect.width,size.y/rect.height);
                size=rect.size*scale;
            }
            var art=(RectTransform)overlay.transform;
            art.anchoredPosition=draw.center;
            art.sizeDelta=size;
            float pixelsToUI=size.x/rect.width;
            paperWidth=(rightCut-leftCut)*pixelsToUI;
            float centerX=((leftCut+rightCut)*.5f-rect.width*.5f)*pixelsToUI;
            viewport.anchoredPosition=new Vector2(centerX,0);
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,size.y);
            var paper=(RectTransform)viewport.Find("Paper");
            SetImage(paper,paperSprite,new Vector2(paperWidth,size.y));
            SetImage(leftRoller,leftSprite,new Vector2(leftCut*pixelsToUI,size.y));
            SetImage(rightRoller,rightSprite,new Vector2((rect.width-rightCut)*pixelsToUI,size.y));
            leftEnd=(leftCut*.5f-rect.width*.5f)*pixelsToUI;
            rightEnd=((rightCut+rect.width)*.5f-rect.width*.5f)*pixelsToUI;
            leftStart=centerX-leftRoller.rect.width*.5f;
            rightStart=centerX+rightRoller.rect.width*.5f;
        }

        private static Sprite Slice(Sprite source,Rect rect) =>
            Sprite.Create(source.texture,rect,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);

        private void SetImage(RectTransform target,Sprite sprite,Vector2 size)
        {
            var image=target.GetComponent<Image>();
            image.sprite=sprite; image.preserveAspect=false;
            image.color=backgroundColor; image.material=background.material;
            target.sizeDelta=size;
        }

        private void Capture()
        {
            int count=contentGroups.Length;
            alphas=new float[count]; interactable=new bool[count]; raycasts=new bool[count];
            for(int i=0;i<count;i++) {
                alphas[i]=contentGroups[i].alpha;
                interactable[i]=contentGroups[i].interactable;
                raycasts[i]=contentGroups[i].blocksRaycasts;
            }
            backgroundColor=background.color;
            captured=true;
        }

        private void ApplyProgress(float value)
        {
            progress=value;
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,paperWidth*value);
            leftRoller.anchoredPosition=new Vector2(Mathf.Lerp(leftStart,leftEnd,value),0);
            rightRoller.anchoredPosition=new Vector2(Mathf.Lerp(rightStart,rightEnd,value),0);
        }

        private void Restore()
        {
            if(captured) {
                for(int i=0;i<contentGroups.Length;i++) if(contentGroups[i]) {
                    contentGroups[i].alpha=alphas[i];
                    contentGroups[i].interactable=interactable[i];
                    contentGroups[i].blocksRaycasts=raycasts[i];
                }
                if(background) background.color=backgroundColor;
            }
            captured=false;
            if(overlay) overlay.gameObject.SetActive(false);
        }

        private void ReleaseSprites()
        {
            if(leftSprite) Destroy(leftSprite);
            if(paperSprite) Destroy(paperSprite);
            if(rightSprite) Destroy(rightSprite);
            sourceSprite=null;
        }

        private void OnDisable() { sequence?.Kill(); sequence=null; Restore(); }
        private void OnDestroy() { sequence?.Kill(); sequence=null; ReleaseSprites(); }
    }
}


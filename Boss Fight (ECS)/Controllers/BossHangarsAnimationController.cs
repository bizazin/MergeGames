using System;
using DG.Tweening;
using Game.Db.UnitSprites;
using Game.Db.UnitSprites.BossFight;
using Ui.Views.Hangar;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ui.Controllers.Impls
{
    public class BossHangarsAnimationController : IBossHangarsAnimationController
    {
        private readonly IBossFightAnimationDatabase _bossFightAnimationDatabase;
        private readonly HangarsView _hangarsView;
        
        private HangarView _currentAttackUnit;
        private float _startAttackUnitPosY;
        private Vector3 _previousItemPos;
        private Vector3 _startFireballPos;

        public BossHangarsAnimationController
        (
            IBossFightAnimationDatabase bossFightAnimationDatabase,
            HangarsView hangarsView
        )
        {
            _bossFightAnimationDatabase = bossFightAnimationDatabase;
            _hangarsView = hangarsView;
        }

        public void SetCurrentUnit(HangarView currentAttackUnit) =>
            _currentAttackUnit = currentAttackUnit;

        public Tween JumpUnit(float jumpHeight, float duration)
        {
            var itemImageTransform = _currentAttackUnit.ItemImage.transform;
            _startAttackUnitPosY = itemImageTransform.position.y;
            return itemImageTransform.DOMoveY(jumpHeight, duration).SetEase(Ease.OutCubic);
        }
        
        public void DisableAllAttackParticles() 
        {
            foreach (var hangar in _hangarsView.BossBattleCollection.GetItems())
                hangar.Fireball.Stop();
        }

        public Tween FireballAppear(float size, float duration)
        {
            var fireball = _currentAttackUnit.Fireball;
            _startFireballPos = fireball.transform.position;
            fireball.Play();
            return fireball.transform.DOScale(size, duration);
        }

        public Tween ThrowFireball(Vector3 bossPosition, float duration) =>
            _currentAttackUnit.Fireball.transform.DOPath(
                new[]
                {
                    GetParabolaVertex(_currentAttackUnit.Fireball.transform.position, bossPosition),
                    bossPosition + (Vector3)(.5f * Random.insideUnitCircle)
                },
                duration, PathType.CatmullRom).SetEase(Ease.InQuart);

        private Vector3 GetParabolaVertex(Vector3 fireballStartPos, Vector3 bossPosition)
        {
            var coordinates = GetCoordinates(fireballStartPos.x, fireballStartPos.y, bossPosition.x, bossPosition.y);
            var x1 = coordinates.Item1.x;
            var y1 = coordinates.Item1.y;
            var x2 = coordinates.Item2.x;
            var y2 = coordinates.Item2.y;
            return y2 > y1
                ? new Vector3(x2, y2, fireballStartPos.z)
                : new Vector3(x1, y1, fireballStartPos.z);
        }
        
        private Tuple<Vector2, Vector2> GetCoordinates(float fromX, float fromY, float toX, float toY)
        {
            var fromXsq = Math.Pow(fromX, 2);
            var fromXcub = Math.Pow(fromX, 3);
            var fromYsq = Math.Pow(fromY, 2);
            var fromYcub = Math.Pow(fromY, 3);
            var toXsq = Math.Pow(toX, 2);
            var toXcub = Math.Pow(toX, 3);
            var toYsq = Math.Pow(toY, 2);
            var toYcub = Math.Pow(toY, 3);
            var sqrt = Math.Sqrt(Math.Pow(_bossFightAnimationDatabase.FireballDeflection,
                2 * Math.Pow(fromY - toY, 2) * (fromXsq - 2 * fromX * toX + fromYsq - 2 * fromY * toY + toXsq + toYsq)));
            var denominator = 2 * (fromXsq - 2 * fromX * toX + fromYsq - 2 * fromY * toY + toXsq + toYsq);
            return Tuple.Create(
                new Vector2(
                    (float)((fromXcub - 2 * sqrt - fromXsq * toX + fromX * fromYsq - 2 * fromX * fromY * toY - fromX * toXsq + fromX * toYsq + fromYsq * toX -
                        2 * fromY * toX * toY + toXcub + toX * toYsq) / denominator),
                    (float)((2 * (fromX - toX) * sqrt + fromXsq * fromYsq - fromXsq * toYsq - 2 * fromX * fromYsq * toX + 2 * fromX * toX * toYsq + Math.Pow(fromY, 4) -
                        2 * fromYcub * toY + fromYsq * toXsq + 2 * fromY * toYcub - toXsq * toYsq - Math.Pow(toY, 4)) / (denominator * (fromY - toY)))),
                new Vector2(
                    (float)((fromXcub + 2 * sqrt - fromXsq * toX + fromX * fromYsq - 2 * fromX * fromY * toY - fromX * toXsq + fromX * toYsq + fromYsq * toX -
                        2 * fromY * toX * toY + toXcub + toX * toYsq) / denominator),
                    (float)((2 * (toX - fromX) * sqrt + fromXsq * fromYsq - fromXsq * toYsq - 2 * fromX * fromYsq * toX + 2 * fromX * toX * toYsq + Math.Pow(fromY, 4) -
                        2 * fromYcub * toY + fromYsq * toXsq + 2 * fromY * toYcub - toXsq * toYsq - Math.Pow(toY, 4)) / (denominator * (fromY - toY)))));
        }

        public Tween ResetUnitPos(float duration)
        {
            var itemImageTransform = _currentAttackUnit.ItemImage.transform;
            return itemImageTransform.DOMoveY(_startAttackUnitPosY, duration).SetEase(Ease.InQuart);
        }

        public Sequence FireballDisappear(float duration)
        {
            var fireball = _currentAttackUnit.Fireball;
            return DOTween.Sequence()
                .Append(fireball.transform.DOScale(0, duration))
                .AppendCallback(() => fireball.Stop())
                .Append(fireball.transform.DOMove(_startFireballPos, 0));
        }
    }
}
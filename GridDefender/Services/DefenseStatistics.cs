using System.Threading;
using Torch;

namespace GVK.GridDefender.Services
{
    public class DefenseStatistics : ViewModel
    {
        private long _totalEvaluated;
        private long _totalBlocked;
        private long _totalAllowed;
        private long _missileHitsAllowed;
        private long _rammingBlocked;
        private long _voxelCrashesBlocked;
        private long _subgridCollisionsBlocked;
        private long _cooldownThrottled;
        private long _clangVibrationsArrested;
        private long _gridsSeparated;

        public long TotalEvaluated => Interlocked.Read(ref _totalEvaluated);
        public long TotalBlocked => Interlocked.Read(ref _totalBlocked);
        public long TotalAllowed => Interlocked.Read(ref _totalAllowed);
        public long MissileHitsAllowed => Interlocked.Read(ref _missileHitsAllowed);
        public long RammingBlocked => Interlocked.Read(ref _rammingBlocked);
        public long VoxelCrashesBlocked => Interlocked.Read(ref _voxelCrashesBlocked);
        public long SubgridCollisionsBlocked => Interlocked.Read(ref _subgridCollisionsBlocked);
        public long CooldownThrottled => Interlocked.Read(ref _cooldownThrottled);
        public long ClangVibrationsArrested => Interlocked.Read(ref _clangVibrationsArrested);
        public long GridsSeparated => Interlocked.Read(ref _gridsSeparated);

        public double BlockRatio
        {
            get
            {
                long eval = TotalEvaluated;
                return eval > 0 ? (double)TotalBlocked / eval * 100.0 : 0.0;
            }
        }

        public void IncrementEvaluated()
        {
            Interlocked.Increment(ref _totalEvaluated);
        }

        public void IncrementBlocked(bool isRamming = false, bool isVoxel = false, bool isSubgrid = false, bool isCooldown = false)
        {
            Interlocked.Increment(ref _totalBlocked);
            if (isRamming) Interlocked.Increment(ref _rammingBlocked);
            if (isVoxel) Interlocked.Increment(ref _voxelCrashesBlocked);
            if (isSubgrid) Interlocked.Increment(ref _subgridCollisionsBlocked);
            if (isCooldown) Interlocked.Increment(ref _cooldownThrottled);
        }

        public void IncrementAllowed(bool isMissile = false)
        {
            Interlocked.Increment(ref _totalAllowed);
            if (isMissile) Interlocked.Increment(ref _missileHitsAllowed);
        }

        public void IncrementClangArrested()
        {
            Interlocked.Increment(ref _clangVibrationsArrested);
        }

        public void IncrementGridsSeparated()
        {
            Interlocked.Increment(ref _gridsSeparated);
        }

        public void NotifyAll()
        {
            OnPropertyChanged(nameof(TotalEvaluated));
            OnPropertyChanged(nameof(TotalBlocked));
            OnPropertyChanged(nameof(TotalAllowed));
            OnPropertyChanged(nameof(BlockRatio));
            OnPropertyChanged(nameof(MissileHitsAllowed));
            OnPropertyChanged(nameof(RammingBlocked));
            OnPropertyChanged(nameof(VoxelCrashesBlocked));
            OnPropertyChanged(nameof(SubgridCollisionsBlocked));
            OnPropertyChanged(nameof(CooldownThrottled));
            OnPropertyChanged(nameof(ClangVibrationsArrested));
            OnPropertyChanged(nameof(GridsSeparated));
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _totalEvaluated, 0);
            Interlocked.Exchange(ref _totalBlocked, 0);
            Interlocked.Exchange(ref _totalAllowed, 0);
            Interlocked.Exchange(ref _missileHitsAllowed, 0);
            Interlocked.Exchange(ref _rammingBlocked, 0);
            Interlocked.Exchange(ref _voxelCrashesBlocked, 0);
            Interlocked.Exchange(ref _subgridCollisionsBlocked, 0);
            Interlocked.Exchange(ref _cooldownThrottled, 0);
            Interlocked.Exchange(ref _clangVibrationsArrested, 0);
            Interlocked.Exchange(ref _gridsSeparated, 0);

            NotifyAll();
        }
    }
}


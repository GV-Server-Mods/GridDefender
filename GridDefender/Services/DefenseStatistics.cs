using System.Threading;
using Torch;

namespace GVK.GridDefender.Services
{
    /// <summary>
    /// Thread-safe telemetry tracker for real-time collision evaluations, blocked crashes, allowed missile hits, and anti-clang interventions.
    /// </summary>
    public class DefenseStatistics : ViewModel
    {
        private long _totalEvaluated;
        private long _totalBlocked;
        private long _totalAllowed;
        private long _missileHitsAllowed;
        private long _rammingBlocked;
        private long _voxelCrashesBlocked;
        private long _subgridCollisionsBlocked;
        private long _lowSpeedBlocked;
        private long _stationBlocked;
        private long _debrisBlocked;
        private long _cooldownThrottled;
        private long _clangVibrationsArrested;
        private long _gridsSeparated;

        /// <summary>
        /// Total number of collision events processed by the defense engine.
        /// </summary>
        public long TotalEvaluated => Interlocked.Read(ref _totalEvaluated);

        /// <summary>
        /// Total number of collision deformations blocked by the engine.
        /// </summary>
        public long TotalBlocked => Interlocked.Read(ref _totalBlocked);

        /// <summary>
        /// Total number of collision deformations allowed (e.g. missiles).
        /// </summary>
        public long TotalAllowed => Interlocked.Read(ref _totalAllowed);

        /// <summary>
        /// Total number of player-made missile impacts allowed through the gate.
        /// </summary>
        public long MissileHitsAllowed => Interlocked.Read(ref _missileHitsAllowed);

        /// <summary>
        /// Total ship-on-ship ramming deformations blocked.
        /// </summary>
        public long RammingBlocked => Interlocked.Read(ref _rammingBlocked);

        /// <summary>
        /// Total ship-vs-voxel (planet/asteroid) crash deformations blocked.
        /// </summary>
        public long VoxelCrashesBlocked => Interlocked.Read(ref _voxelCrashesBlocked);

        /// <summary>
        /// Total mechanical subgrid self-deformations blocked.
        /// </summary>
        public long SubgridCollisionsBlocked => Interlocked.Read(ref _subgridCollisionsBlocked);

        /// <summary>
        /// Total low-speed driving and safe docking bumps blocked (below MinDrivingVelocity).
        /// </summary>
        public long LowSpeedBlocked => Interlocked.Read(ref _lowSpeedBlocked);

        /// <summary>
        /// Total non-missile collisions with static stations blocked.
        /// </summary>
        public long StationBlocked => Interlocked.Read(ref _stationBlocked);

        /// <summary>
        /// Total loose floating debris and ore impacts blocked.
        /// </summary>
        public long DebrisBlocked => Interlocked.Read(ref _debrisBlocked);

        /// <summary>
        /// Total deformations throttled by cooldown frames.
        /// </summary>
        public long CooldownThrottled => Interlocked.Read(ref _cooldownThrottled);

        /// <summary>
        /// Total high-frequency physics oscillations / death spins dampened by Anti-Clang.
        /// </summary>
        public long ClangVibrationsArrested => Interlocked.Read(ref _clangVibrationsArrested);

        /// <summary>
        /// Total persistent stuck/phased grid pairs separated via Push-Apart.
        /// </summary>
        public long GridsSeparated => Interlocked.Read(ref _gridsSeparated);

        /// <summary>
        /// Percentage of evaluated collisions that were blocked (0.0% to 100.0%).
        /// </summary>
        public double BlockRatio
        {
            get
            {
                long eval = TotalEvaluated;
                return eval > 0 ? (double)TotalBlocked / eval * 100.0 : 0.0;
            }
        }

        /// <summary>
        /// Increments the total evaluated collisions counter.
        /// </summary>
        public void IncrementEvaluated()
        {
            Interlocked.Increment(ref _totalEvaluated);
        }

        /// <summary>
        /// Increments blocked crash counters with granular category tracking.
        /// </summary>
        public void IncrementBlocked(
            bool isRamming = false,
            bool isVoxel = false,
            bool isSubgrid = false,
            bool isCooldown = false,
            bool isLowSpeed = false,
            bool isStation = false,
            bool isDebris = false)
        {
            Interlocked.Increment(ref _totalBlocked);
            if (isRamming) Interlocked.Increment(ref _rammingBlocked);
            if (isVoxel) Interlocked.Increment(ref _voxelCrashesBlocked);
            if (isSubgrid) Interlocked.Increment(ref _subgridCollisionsBlocked);
            if (isCooldown) Interlocked.Increment(ref _cooldownThrottled);
            if (isLowSpeed) Interlocked.Increment(ref _lowSpeedBlocked);
            if (isStation) Interlocked.Increment(ref _stationBlocked);
            if (isDebris) Interlocked.Increment(ref _debrisBlocked);
        }

        /// <summary>
        /// Increments allowed deformation counters.
        /// </summary>
        public void IncrementAllowed(bool isMissile = false)
        {
            Interlocked.Increment(ref _totalAllowed);
            if (isMissile) Interlocked.Increment(ref _missileHitsAllowed);
        }

        /// <summary>
        /// Increments the count of arrested Clang vibrations / death spins.
        /// </summary>
        public void IncrementClangArrested()
        {
            Interlocked.Increment(ref _clangVibrationsArrested);
        }

        /// <summary>
        /// Increments the count of grids separated via Push-Apart.
        /// </summary>
        public void IncrementGridsSeparated()
        {
            Interlocked.Increment(ref _gridsSeparated);
        }

        /// <summary>
        /// Notifies the WPF UI of property changes across all telemetry metrics.
        /// </summary>
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
            OnPropertyChanged(nameof(LowSpeedBlocked));
            OnPropertyChanged(nameof(StationBlocked));
            OnPropertyChanged(nameof(DebrisBlocked));
            OnPropertyChanged(nameof(CooldownThrottled));
            OnPropertyChanged(nameof(ClangVibrationsArrested));
            OnPropertyChanged(nameof(GridsSeparated));
        }

        /// <summary>
        /// Resets all collision and telemetry counters to zero.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _totalEvaluated, 0);
            Interlocked.Exchange(ref _totalBlocked, 0);
            Interlocked.Exchange(ref _totalAllowed, 0);
            Interlocked.Exchange(ref _missileHitsAllowed, 0);
            Interlocked.Exchange(ref _rammingBlocked, 0);
            Interlocked.Exchange(ref _voxelCrashesBlocked, 0);
            Interlocked.Exchange(ref _subgridCollisionsBlocked, 0);
            Interlocked.Exchange(ref _lowSpeedBlocked, 0);
            Interlocked.Exchange(ref _stationBlocked, 0);
            Interlocked.Exchange(ref _debrisBlocked, 0);
            Interlocked.Exchange(ref _cooldownThrottled, 0);
            Interlocked.Exchange(ref _clangVibrationsArrested, 0);
            Interlocked.Exchange(ref _gridsSeparated, 0);

            NotifyAll();
        }
    }
}


using Swift;
using Swift.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SCM
{
    public class UnitUtils
    {
        public static UnitConfigInfo ReadUnitInfo(IReadableBuffer reader)
        {
            var info = new UnitConfigInfo();

            info.DisplayName = reader.ReadString();
            info.Cost = reader.ReadInt();
            info.GasCost = reader.ReadInt();
            info.ConstructingTime = reader.ReadFix64();
            info.MaxNum = reader.ReadInt();
            info.MaxVelocity = reader.ReadFix64();
            info.IsBuilding = reader.ReadBool();
            info.IsBiological = reader.ReadBool();
            info.IsMechanical = reader.ReadBool();
            info.IsAirUnit = reader.ReadBool();
            info.UnAttackable = reader.ReadBool();
            info.VisionRadius = reader.ReadFix64();
            info.ChaseRadius = reader.ReadFix64();
            info.Suppliment = reader.ReadFix64();
            info.AITypes = reader.ReadStringArr();

            if (reader.ReadBool())
            {
                var len2 = reader.ReadInt();
                info.AIParams = new Fix64[len2][];
                for (var i = 0; i < len2; i++)
                {
                    if (reader.ReadBool())
                    {
                        var len = reader.ReadInt();
                        info.AIParams[i] = new Fix64[len];
                        for (var j = 0; j < len; j++)
                            info.AIParams[i][j] = reader.ReadFix64();
                    }
                }
            }

            info.SizeRadius = reader.ReadInt();
            info.NoBody = reader.ReadBool();
            info.NoCard = reader.ReadBool();
            info.MaxHp = reader.ReadInt();
            info.CanAttackGround = reader.ReadBool();
            info.CanAttackAir = reader.ReadBool();

            info.AttackType = reader.ReadStringArr();
            info.AttackRange = reader.ReadIntArr();
            info.AttackPower = reader.ReadIntArr();

            if (reader.ReadBool())
            {
                var len6 = reader.ReadInt();
                info.AttackInterval = new Fix64[len6];

                for (var i = 0; i < len6; i++)
                    info.AttackInterval[i] = reader.ReadFix64();
            }

            info.AOEType = reader.ReadStringArr();

            if (reader.ReadBool())
            {
                var len8 = reader.ReadInt();
                info.AOEParams = new Fix64[len8][];
                for (var i = 0; i < len8; i++)
                {
                    if (reader.ReadBool())
                    {
                        var len = reader.ReadInt();
                        info.AOEParams [i] = new Fix64[len];
                        for (var j = 0; j < len; j++)
                            info.AOEParams[i][j] = reader.ReadFix64();
                    }
                }
            }

            info.ArmorType = reader.ReadString();
            info.Defence = reader.ReadInt();

            if (reader.ReadBool())
            {
                var len9 = reader.ReadInt();
                info.Prerequisites = new string[len9][];
                for (var i = 0; i < len9; i++)
                {
                    if (reader.ReadBool())
                    {
                        var len = reader.ReadInt();
                        info.Prerequisites[i] = new string[len];
                        for (var j = 0; j < len; j++)
                        {
                            info.Prerequisites [i] [j] = reader.ReadString();
                        }
                    }
                }
            }

            info.ReconstructTo = reader.ReadStringArr();
            info.ReconstructFrom = reader.ReadString();
            info.TechLevel = reader.ReadInt();
            info.Desc = reader.ReadString();
            info.InVisible = reader.ReadBool();
            info.IsObserver = reader.ReadBool();
            info.ReboundDamage = reader.ReadFix64();
            info.Pets = reader.ReadStringArr();
            info.OriginalType = reader.ReadString();
            info.IsThirdType = reader.ReadBool();

            return info;
        }

        public static void WriteUnitInfo(UnitConfigInfo info, IWriteableBuffer writer)
        {
            writer.Write(info.DisplayName);
            writer.Write(info.Cost);
            writer.Write(info.GasCost);
            writer.Write(info.ConstructingTime);
            writer.Write(info.MaxNum);
            writer.Write(info.MaxVelocity);
            writer.Write(info.IsBuilding);
            writer.Write(info.IsBiological);
            writer.Write(info.IsMechanical);
            writer.Write(info.IsAirUnit);
            writer.Write(info.UnAttackable);
            writer.Write(info.VisionRadius);
            writer.Write(info.ChaseRadius);
            writer.Write(info.Suppliment);
            writer.Write(info.AITypes);

            bool hasAIParams = info.AIParams != null;
            writer.Write(hasAIParams);
            if (hasAIParams)
            {
                var len2 = info.AIParams.Length;
                writer.Write(len2);
                for (var i = 0; i < len2; i++)
                {
                    writer.Write(info.AIParams [i] != null);

                    if (info.AIParams[i] != null)
                    {
                        var len = info.AIParams[i].Length;
                        writer.Write(len);
                        for (var j = 0; j < len; j++)
                            writer.Write(info.AIParams[i][j]);
                    }
                }
            }

            writer.Write(info.SizeRadius);
            writer.Write(info.NoBody);
            writer.Write(info.NoCard);
            writer.Write(info.MaxHp);
            writer.Write(info.CanAttackGround);
            writer.Write(info.CanAttackAir);
            writer.Write(info.AttackType);
            writer.Write(info.AttackRange);
            writer.Write(info.AttackPower);

            var hasAttackInterval = info.AttackInterval != null;
            writer.Write(hasAttackInterval);
            if (hasAttackInterval)
            {
                var len6 = info.AttackInterval.Length;
                writer.Write(len6);
                for (var i = 0; i < len6; i++)
                    writer.Write(info.AttackInterval[i]);
            }

            writer.Write(info.AOEType);

            bool hasAOEParams = info.AOEParams != null;
            writer.Write(hasAOEParams);
            if (hasAOEParams)
            {
                var len2 = info.AOEParams.Length;
                writer.Write(len2);
                for (var i = 0; i < len2; i++)
                {
                    writer.Write(info.AOEParams [i] != null);

                    if (info.AOEParams[i] != null)
                    {
                        var len = info.AOEParams[i].Length;
                        writer.Write(len);
                        for (var j = 0; j < len; j++)
                            writer.Write(info.AOEParams[i][j]);
                    }
                }
            }

            writer.Write(info.ArmorType);
            writer.Write(info.Defence);

            var hasPrerequisites = info.Prerequisites != null;
            writer.Write(hasPrerequisites);
            if (hasPrerequisites)
            {
                var len9 = info.Prerequisites.Length;
                writer.Write(len9);
                for (var i = 0; i < len9; i++)
                {
                    writer.Write(info.Prerequisites [i] != null);
                    if (info.Prerequisites [i] != null)
                    {
                        var len = info.Prerequisites [i].Length;
                        writer.Write(len);
                        for (int j = 0; j < len; j++)
                        {
                            writer.Write(info.Prerequisites[i][j]);
                        }
                    }
                }
            }

            writer.Write(info.ReconstructTo);
            writer.Write(info.ReconstructFrom);
            writer.Write(info.TechLevel);
            writer.Write(info.Desc);
            writer.Write(info.InVisible);
            writer.Write(info.IsObserver);
            writer.Write(info.ReboundDamage);
            writer.Write(info.Pets);
            writer.Write(info.OriginalType);
            writer.Write(info.IsThirdType);
        }
    }
}


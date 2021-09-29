using Bencodex;
using Bencodex.Types;
using Cocona;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Tx
    {
        private static Codec _codec = new Codec();

        [Command(Description = "Create TransferAsset action and dump it.")]
        public void TransferAsset(
            [Argument("SENDER", Description = "An address of sender.")] string sender,
            [Argument("RECIPIENT", Description = "An address of recipient.")] string recipient,
            [Argument("AMOUNT", Description = "An amount of gold to transfer.")] int goldAmount,
            [Argument("GENESIS-BLOCK", Description = "A genesis block containing InitializeStates.")] string genesisBlock
        )
        {
            byte[] genesisBytes = File.ReadAllBytes(genesisBlock);
            var genesisDict = (Bencodex.Types.Dictionary)_codec.Decode(genesisBytes);
            IReadOnlyList<Transaction<NCAction>> genesisTxs =
                BlockMarshaler.UnmarshalBlockTransactions<NCAction>(genesisDict);
            var initStates = (InitializeStates)genesisTxs.Single().Actions.Single().InnerAction;
            Currency currency = new GoldCurrencyState(initStates.GoldCurrency).Currency;

            var action = new TransferAsset(
                new Address(sender),
                new Address(recipient),
                currency * goldAmount
            );

            var bencoded = new List(
                new IValue[]
                {
                    (Text) nameof(TransferAsset),
                    action.PlainValue
                }
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.Write(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create new transaction with given actions and dump it.")]
        public void Sign(
            [Argument("PRIVATE-KEY", Description = "A hex-encoded private key for signing.")] string privateKey,
            [Argument("NONCE", Description = "A nonce for new transaction.")] long nonce,
            [Argument("TIMESTAMP", Description = "A datetime for new transaction.")] string timestamp = null,
            [Argument("GENESIS-HASH", Description = "A hex-encoded genesis block hash.")] string genesisHash = null,
            [Option("action", new[] { 'a' }, Description = "Hex-encoded actions or a path of the file contained it.")] string[] actions = null,
            [Option("bytes", new[] { 'b' }, Description = "Print raw bytes instead of base64.  No trailing LF appended.")] bool bytes = false
        )
        {
            List<NCAction> parsedActions = null;
            if (!(actions is null))
            {
                parsedActions = actions.Select(a =>
                {
                    if (File.Exists(a))
                    {
                        a = File.ReadAllText(a);
                    }

                    var bencoded = (List)_codec.Decode(ByteUtil.ParseHex(a));
                    string type = (Text) bencoded[0];
                    Dictionary plainValue = (Dictionary)bencoded[1];

                    ActionBase action = null;
                    action = type switch
                    {
                        nameof(TransferAsset) => new TransferAsset(),
                        nameof(PatchTableSheet) => new PatchTableSheet(),
                        nameof(AddRedeemCode) => new AddRedeemCode(),
                        nameof(Nekoyume.Action.MigrationLegacyShop) => new MigrationLegacyShop(),
                        nameof(Nekoyume.Action.MigrationActivatedAccountsState) => new MigrationActivatedAccountsState(),
                        nameof(Nekoyume.Action.MigrationAvatarState) => new MigrationAvatarState(),
                        _ => throw new CommandExitedException($"Can't determine given action type: {type}", 128),
                    };
                    action.LoadPlainValue(plainValue);

                    return (NCAction)action;
                }).ToList();
            }
            Transaction<NCAction> tx = Transaction<NCAction>.Create(
                nonce: nonce,
                privateKey: new PrivateKey(ByteUtil.ParseHex(privateKey)),
                genesisHash: (genesisHash is null) ? default : BlockHash.FromString(genesisHash),
                timestamp: (timestamp is null) ? default : DateTimeOffset.Parse(timestamp),
                actions: parsedActions
            );
            byte[] raw = tx.Serialize(true);

            if (bytes)
            {
                using Stream stdout = Console.OpenStandardOutput();
                stdout.Write(raw);
            }
            else
            {
                Console.WriteLine(Convert.ToBase64String(raw));
            }
        }

        [Command(Description = "Create PatchTable action and dump it.")]
        public void PatchTable(
            [Argument("TABLE-PATH", Description = "A table file path for patch.")]
            string tablePath
        )
        {
            var tableName = Path.GetFileName(tablePath);
            if (tableName.EndsWith(".csv"))
            {
                tableName = tableName.Split(".csv")[0];
            }
            Console.Error.Write("----------------\n");
            Console.Error.Write(tableName);
            Console.Error.Write("\n----------------\n");
            var tableCsv = File.ReadAllText(tablePath);
            Console.Error.Write(tableCsv);
            var action = new PatchTableSheet
            {
                TableName = tableName,
                TableCsv = tableCsv
            };

            var bencoded = new List(
                new IValue[]
                {
                    (Text) nameof(PatchTableSheet),
                    action.PlainValue
                }
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create MigrationLegacyShop action and dump it.")]
        public void MigrationLegacyShop()
        {
            var action = new MigrationLegacyShop();

            var bencoded = new List(
                new IValue[]
                {
                    (Text) nameof(Nekoyume.Action.MigrationLegacyShop),
                    action.PlainValue
                }
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create MigrationActivatedAccountsState action and dump it.")]
        public void MigrationActivatedAccountsState()
        {
            var action = new MigrationActivatedAccountsState();
            var bencoded = new List(
                new IValue[]
                {
                    (Text) nameof(Nekoyume.Action.MigrationActivatedAccountsState),
                    action.PlainValue
                }
            );

            byte[] raw = _codec.Encode(bencoded);
            Console.WriteLine(ByteUtil.Hex(raw));
        }

        [Command(Description = "Create MigrationAvatarState action and dump it.")]
        public void MigrationAvatarState(
        [Argument("directory-path", Description = "path of the directory contained hex-encoded avatar states.")] string directoryPath,
        [Argument("output-path", Description = "path of the output file dumped action.")] string outputPath
        )
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            var avatarStates = files.Select(a =>
            {
                var raw = File.ReadAllText(a);
                return (Dictionary)_codec.Decode(ByteUtil.ParseHex(raw));
            }).ToList();
            var action = new MigrationAvatarState()
            {
                avatarStates = avatarStates
            };

            var encoded = new List(
                new IValue[]
                {
                    (Text) nameof(Nekoyume.Action.MigrationAvatarState),
                    action.PlainValue
                }
            );

            byte[] raw = _codec.Encode(encoded);
            File.WriteAllText(outputPath, ByteUtil.Hex(raw));
        }

        [Command(Description = "Create new transaction with AddRedeemCode action and dump it.")]
        public void AddRedeemCode(
            [Argument("PRIVATE-KEY", Description = "A hex-encoded private key for signing.")] string privateKey,
            [Argument("NONCE", Description = "A nonce for new transaction.")] long nonce,
            [Argument("TABLE-PATH", Description = "A table file path for RedeemCodeListSheet")] string tablePath,
            [Argument("GENESIS-HASH", Description = "A hex-encoded genesis block hash.")] string genesisHash
        )
        {
            var tableCsv = File.ReadAllText(tablePath);
            var action = new AddRedeemCode
            {
                redeemCsv = tableCsv
            };

            Transaction<NCAction> tx = Transaction<NCAction>.Create(
                nonce: nonce,
                privateKey: new PrivateKey(ByteUtil.ParseHex(privateKey)),
                genesisHash: (genesisHash is null) ? default : BlockHash.FromString(genesisHash),
                timestamp: default,
                actions: new List<NCAction>{ action }
            );
            byte[] raw = tx.Serialize(true);

            Console.WriteLine(ByteUtil.Hex(raw));
        }
    }
}

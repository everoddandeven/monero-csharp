using Monero.Common;
using Monero.Test.Utils;
using Monero.Wallet.Common;

namespace Monero.Test;

public class TestMoneroWalletModel
{

    [Fact]
    public void TestMoneroTxQuery()
    {
        var query = new MoneroTxQuery();
        
        AssertMembersNull(query);

        var copy = query.Clone();
        
        AssertMembersNull(copy);

        query.SetIsOutgoing(false);
        Assert.False(query.IsOutgoing());
        query.SetIsIncoming(true);
        Assert.True(query.IsIncoming());
        query.SetHashes(["hash"]);
        Assert.NotNull(query.GetHashes());
        Assert.NotEmpty(query.GetHashes());
        query.SetPaymentId("paymentId");
        Assert.NotNull(query.GetPaymentIds());
        Assert.NotEmpty(query.GetPaymentIds());
        Assert.Equal(query.GetPaymentIds()[0], "paymentId");
        query.SetHasPaymentId(true);
        Assert.True(query.HasPaymentId());
        query.SetHeight(TestUtils.FIRST_RECEIVE_HEIGHT);
        Assert.NotNull(query.GetHeight());
        Assert.Equal(TestUtils.FIRST_RECEIVE_HEIGHT, query.GetHeight());
        query.SetMinHeight(TestUtils.FIRST_RECEIVE_HEIGHT - 10);
        Assert.NotNull(query.GetMinHeight());
        Assert.Equal(TestUtils.FIRST_RECEIVE_HEIGHT - 10, query.GetMinHeight());
        query.SetMaxHeight(TestUtils.FIRST_RECEIVE_HEIGHT + 10);
        Assert.NotNull(query.GetMaxHeight());
        Assert.Equal(TestUtils.FIRST_RECEIVE_HEIGHT + 10, query.GetMaxHeight());
        query.SetIncludeOutputs(false);
        Assert.False(query.GetIncludeOutputs());

        var transferQuery = new MoneroTransferQuery();
        var inputQuery = new MoneroOutputQuery();
        var outputQuery = new MoneroOutputQuery();

        query.SetTransferQuery(transferQuery);
        Assert.Equal(transferQuery, query.GetTransferQuery());
        query.SetInputQuery(inputQuery);
        Assert.Equal(inputQuery, query.GetInputQuery());
        query.SetOutputQuery(outputQuery);
        Assert.Equal(outputQuery, query.GetOutputQuery());

        copy = query.Clone();
        
        AssertEqual(query, copy);
        AssertEqual(query.GetInputQuery(), copy.GetInputQuery());
        AssertEqual(query.GetOutputQuery(), copy.GetOutputQuery());
        AssertEqual(query.GetTransferQuery(), copy.GetTransferQuery());
    }

    private static void AssertMembersNull(MoneroTxQuery query)
    {
        Assert.Null(query.IsOutgoing());
        Assert.Null(query.IsIncoming());
        Assert.Null(query.GetHashes());
        Assert.Null(query.HasPaymentId());
        Assert.Null(query.GetPaymentIds());
        Assert.Null(query.GetHeight());
        Assert.Null(query.GetMinHeight());
        Assert.Null(query.GetMaxHeight());
        Assert.Null(query.GetIncludeOutputs());
        Assert.Null(query.GetTransferQuery());
        Assert.Null(query.GetInputQuery());
        Assert.Null(query.GetOutputQuery());
    }

    private static void AssertEqual(MoneroTxQuery? query1, MoneroTxQuery? query2)
    {
        if (query1 == query2) return;
        Assert.NotNull(query1);
        Assert.NotNull(query2);
        Assert.Equal(query1.IsOutgoing(), query2.IsOutgoing());
        Assert.Equal(query1.IsIncoming(), query2.IsIncoming());
        Assert.Equal(query1.GetHashes(), query2.GetHashes());
        Assert.Equal(query1.HasPaymentId(), query2.HasPaymentId());
        Assert.Equal(query1.GetPaymentIds(), query2.GetPaymentIds());
        Assert.Equal(query1.GetHeight(), query2.GetHeight());
        Assert.Equal(query1.GetMinHeight(),  query2.GetMinHeight());
        Assert.Equal(query1.GetMaxHeight(),  query2.GetMaxHeight());
        Assert.Equal(query1.GetIncludeOutputs(), query2.GetIncludeOutputs());
    }

    private static void AssertEqual(MoneroTransferQuery? query1, MoneroTransferQuery? query2)
    {
        if (query1 == query2) return;
        Assert.NotNull(query1);
        Assert.NotNull(query2);
    }

    private static void AssertEqual(MoneroKeyImage? k1, MoneroKeyImage? k2)
    {
        if (k1 == k2) return;
        Assert.NotNull(k1);
        Assert.NotNull(k2);
        Assert.Equal(k1.GetHex(), k2.GetHex());
        Assert.Equal(k1.GetSignature(), k2.GetSignature());
    }
    
    private static void AssertEqual(MoneroOutput? o1, MoneroOutput? o2)
    {
        if (o1 == o2) return;
        Assert.NotNull(o1);
        Assert.NotNull(o2);
        AssertEqual(o1.GetKeyImage(), o2.GetKeyImage());
        Assert.Equal(o1.GetAmount(), o2.GetAmount());
        Assert.Equal(o1.GetIndex(), o2.GetIndex());
        Assert.Equal(o1.GetStealthPublicKey(), o2.GetStealthPublicKey());
        Assert.Equal(o1.GetRingOutputIndices(), o2.GetRingOutputIndices());
    }
    
    private static void AssertEqual(MoneroOutputWallet? o1, MoneroOutputWallet? o2)
    {
        if (o1 == o2) return;
        
        Assert.NotNull(o1);
        Assert.NotNull(o2);
        
        AssertEqual((MoneroOutput)o1, (MoneroOutput)o2);
        
        Assert.Equal(o1.GetAccountIndex(), o2.GetAccountIndex());
        Assert.Equal(o1.GetSubaddressIndex(), o2.GetSubaddressIndex());
        Assert.Equal(o1.IsFrozen(), o2.IsFrozen());
        Assert.Equal(o2.IsSpent(), o2.IsSpent());
    }

    private static void AssertEqual(MoneroOutputQuery? query1, MoneroOutputQuery? query2)
    {
        if (query1 == null && query2 == null) return;
        Assert.NotNull(query1);
        Assert.NotNull(query2);
        
        AssertEqual((MoneroOutputWallet)query1, (MoneroOutputWallet)query2);
        
        Assert.Equal(query1.GetSubaddressIndices(), query2.GetSubaddressIndices());
        Assert.Equal(query1.GetMinAmount(), query2.GetMinAmount());
        Assert.Equal(query1.GetMaxAmount(), query2.GetMaxAmount());
    }


    
}
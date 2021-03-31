<%@ Page Title="About" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="WebApplication2.About" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%: Title %>.</h2>
    <h3>Program 4 by Sonlam Nguyen</h3>
    <p>
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Load Data" />
        <p>
            <asp:Label ID="Label2" runat="server" Text="All Data is loaded from https://s3-us-west-2.amazonaws.com/css490/input.txt"></asp:Label>
    <hr>

    </p>
    <p>
        <asp:Button ID="Button2" runat="server" Text="Clear Data" OnClick="Button2_Click" />
        <p>
            <asp:Label ID="Label3" runat="server" Text=""></asp:Label>
        <hr>

    <p>
        <asp:TextBox ID="TextBox1" runat="server" OnTextChanged="TextBox1_TextChanged"></asp:TextBox>
        <asp:TextBox ID="TextBox2" runat="server" OnTextChanged="TextBox2_TextChanged"></asp:TextBox>

    </p>
    <p>
        LastName&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; FirstName</p>
    <p>
        <asp:Button ID="Button3" runat="server" OnClick="Button3_Click" Text="Query" />

    </p>
    <p>
        <asp:Label ID="Label1" runat="server" Text="Quest results appear here"></asp:Label>

    </p>
</asp:Content>

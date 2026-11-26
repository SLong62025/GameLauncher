using System;
using System.Collections.Generic;
using System.Text;

namespace Launcher.Providers;

//Vdf reader class because Valve made their own text format
internal sealed class VdfNode {
    public string? Value { get; init; }

    public Dictionary<string, VdfNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? GetString(string key) =>
        Children.TryGetValue(key, out var child) ? child.Value : null;

    public VdfNode? GetNode(string key) =>
        Children.TryGetValue(key, out var child) && child.Value is null ? child : null;
}

internal static class VdfParser {
    private enum TokenKind { Text, Open, Close }

    private readonly record struct Token(TokenKind Kind, string Text);

    public static VdfNode Parse(string text)
    {
        var tokens = Tokenize(text);
        var pos = 0;
        var root = new VdfNode();
        ReadBlock(tokens, ref pos, root, isRoot: true);
        return root;
    }

    private static void ReadBlock(List<Token> tokens, ref int pos, VdfNode node, bool isRoot) {
        while (pos < tokens.Count) {
            var key = tokens[pos++];

            if (key.Kind == TokenKind.Close) {
                if (isRoot) throw new FormatException("Unexpected '}' in VDF.");
                return;
            }

            if (key.Kind == TokenKind.Open)
                throw new FormatException("Unexpected '{' in VDF.");

            if (pos >= tokens.Count)
                throw new FormatException($"Key '{key.Text}' has no value.");

            var next = tokens[pos++];
            switch (next.Kind) {
                case TokenKind.Open:
                    var child = new VdfNode();
                    ReadBlock(tokens, ref pos, child, isRoot: false);
                    node.Children[key.Text] = child;
                    break;

                case TokenKind.Text:
                    node.Children[key.Text] = new VdfNode { Value = next.Text };
                    break;

                default:
                    throw new FormatException($"Key '{key.Text}' has no value.");
            }
        }

        if (!isRoot) throw new FormatException("Missing '}' in VDF.");
    }

    private static List<Token> Tokenize(string text) {
        var tokens = new List<Token>();
        var i = 0;

        while (i < text.Length) {
            var c = text[i];

            if (char.IsWhiteSpace(c) || c == '﻿') {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/') {
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }

            if (c == '{') {
                tokens.Add(new Token(TokenKind.Open, "{"));
                i++;
                continue;
            }

            if (c == '}') {
                tokens.Add(new Token(TokenKind.Close, "}"));
                i++;
                continue;
            }

            if (c == '"') {
                i++;
                var sb = new StringBuilder();
                while (i < text.Length && text[i] != '"') {
                    if (text[i] == '\\' && i + 1 < text.Length) {
                        i++;
                        switch (text[i]) {
                            case 'n': sb.Append('\n'); break;
                            case 't': sb.Append('\t'); break;
                            case '\\': sb.Append('\\'); break;
                            case '"': sb.Append('"'); break;
                            default: sb.Append('\\').Append(text[i]); break;
                        }
                    }
                    else
                    {
                        sb.Append(text[i]);
                    }
                    i++;
                }

                if (i >= text.Length) throw new FormatException();
                i++; // closing quote
                tokens.Add(new Token(TokenKind.Text, sb.ToString()));
                continue;
            }
            
            var start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]) &&
                   text[i] != '{' && text[i] != '}' && text[i] != '"') {
                i++;
            }
            tokens.Add(new Token(TokenKind.Text, text[start..i]));
        }

        return tokens;
    }
}

import React from "react";

// VS Code dark+ inspired colors
const COLORS = {
  keyword: "#569cd6",
  type: "#4ec9b0",
  string: "#ce9178",
  number: "#b5cea8",
  comment: "#6a9955",
  method: "#dcdcaa",
  param: "#9cdcfe",
  punctuation: "#d4d4d4",
  default: "#d4d4d4",
};

const KEYWORDS = new Set([
  "var", "int", "double", "void", "for", "if", "else", "return", "new",
  "using", "true", "false", "null", "string", "bool", "float", "long",
]);

const TYPES = new Set([
  "Color", "DrawingContext", "BlendMode", "Math", "Colors",
]);

interface Token {
  text: string;
  color: string;
}

function tokenize(code: string): Token[] {
  const tokens: Token[] = [];
  let i = 0;

  while (i < code.length) {
    // Line comments
    if (code[i] === "/" && code[i + 1] === "/") {
      let end = code.indexOf("\n", i);
      if (end === -1) end = code.length;
      tokens.push({ text: code.slice(i, end), color: COLORS.comment });
      i = end;
      continue;
    }

    // Strings (double-quoted, including interpolated $"...")
    if (code[i] === '"' || (code[i] === "$" && code[i + 1] === '"')) {
      const start = i;
      if (code[i] === "$") i++;
      i++; // skip opening quote
      while (i < code.length && code[i] !== '"') {
        if (code[i] === "\\") i++; // skip escaped char
        i++;
      }
      i++; // skip closing quote
      tokens.push({ text: code.slice(start, i), color: COLORS.string });
      continue;
    }

    // Numbers
    if (/[0-9]/.test(code[i]) && (i === 0 || /[\s(,=+\-*/<>!&|;{[]/.test(code[i - 1]))) {
      const start = i;
      while (i < code.length && /[0-9.]/.test(code[i])) i++;
      tokens.push({ text: code.slice(start, i), color: COLORS.number });
      continue;
    }

    // Words (identifiers, keywords, types)
    if (/[a-zA-Z_]/.test(code[i])) {
      const start = i;
      while (i < code.length && /[a-zA-Z0-9_]/.test(code[i])) i++;
      const word = code.slice(start, i);

      if (KEYWORDS.has(word)) {
        tokens.push({ text: word, color: COLORS.keyword });
      } else if (TYPES.has(word)) {
        tokens.push({ text: word, color: COLORS.type });
      } else if (i < code.length && code[i] === "(") {
        tokens.push({ text: word, color: COLORS.method });
      } else {
        tokens.push({ text: word, color: COLORS.param });
      }
      continue;
    }

    // Everything else (whitespace, operators, punctuation)
    tokens.push({ text: code[i], color: COLORS.punctuation });
    i++;
  }

  return tokens;
}

export const CodeHighlight: React.FC<{
  code: string;
  fontSize: number;
  lineHeight: number;
}> = ({ code, fontSize, lineHeight }) => {
  const tokens = tokenize(code);

  return (
    <span>
      {tokens.map((token, i) => (
        <span key={i} style={{ color: token.color }}>
          {token.text}
        </span>
      ))}
    </span>
  );
};

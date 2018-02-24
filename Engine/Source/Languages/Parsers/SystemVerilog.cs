using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;

namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
{
	public class SystemVerilog : Language
	{
		public SystemVerilog (Languages.Manager manager) : base (manager, "SystemVerilog")
		{
			Type = LanguageType.FullSupport;

			LineCommentStrings = new string[] { "//" };
			BlockCommentStringPairs = new string[] { "/*", "*/" };
			JavadocBlockCommentStringPairs = new string[] { "/**", "*/" };
			XMLLineCommentStrings = new string[] { "///" };

			MemberOperator = ".";
			EnumValue = EnumValues.UnderType;
			CaseSensitive = true;
		}

		override public List<Element> GetCodeElements (Tokenizer source)
		{
			List<Element> elements = new List<Element>();

			// Having a root element is important for setting the default child access level and to provide a target for top-level
			// using statements.
			ParentElement rootElement = new ParentElement(0, 0, Element.Flags.InCode);
			rootElement.IsRootElement = true;
			rootElement.MaximumEffectiveChildAccessLevel = AccessLevel.Public;
			rootElement.DefaultDeclaredChildAccessLevel = AccessLevel.Internal;
			rootElement.DefaultChildLanguageID = this.ID;
			rootElement.ChildContextString = new ContextString();
			rootElement.EndingLineNumber = int.MaxValue;
			rootElement.EndingCharNumber = int.MaxValue;

			elements.Add(rootElement);

			TokenIterator iterator = source.FirstToken;
			GetCodeElements(ref iterator, elements, new SymbolString());

			return elements;
		}


		protected void GetCodeElements (ref TokenIterator iterator, List<Element> elements, SymbolString scope,
			char untilAfterChar = '\0')
		{
			while (iterator.IsInBounds)
			{
				System.Console.WriteLine("66666");
				System.Console.WriteLine(iterator.Tokenizer.RawText);
				if (iterator.Character == untilAfterChar)
				{
					iterator.Next();
					break;
				}

				else if (TryToSkipWhitespace(ref iterator) //||
					//TryToSkipPreprocessingDirective(ref iterator) ||

					//TryToSkipUsingStatement(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipNamespace(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipClass(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipFunction(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipVariable(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipProperty(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipConstructor(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipEnum(ref iterator, ParseMode.CreateElements, elements, scope) ||
					//TryToSkipConversionOperator(ref iterator, ParseMode.CreateElements, elements, scope) ||

					// We skip attributes after trying to get language elements because they may be part of one.
					// We have to skip attributes in this loop to begin with because they don't end like regular statements, so not having
					// this here would mean SkipRestOfStatement() skips it and the next statement.
					// We only skip a single attribute because a global one may be followed by a local one that's part of a language element.
					/*TryToSkipAttribute(ref iterator)*/)
				{  }

				else
				{  SkipRestOfStatement(ref iterator);  }
			}
		}

		protected bool TryToSkipWhitespace (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			int originalTokenIndex = iterator.TokenIndex;

			for (;;)
			{
				if (iterator.FundamentalType == FundamentalType.Whitespace ||
					iterator.FundamentalType == FundamentalType.LineBreak)
				{  iterator.Next();  }

				else if (TryToSkipComment(ref iterator, mode))
				{  }

				else
				{  break;  }
			}

			return (iterator.TokenIndex != originalTokenIndex);
		}

		new protected bool TryToSkipComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			return (TryToSkipLineComment(ref iterator, mode) ||
				TryToSkipBlockComment(ref iterator, mode));
		}


		new protected bool TryToSkipLineComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			if (iterator.MatchesAcrossTokens("//"))
			{
				TokenIterator startOfComment = iterator;
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
					iterator.FundamentalType != FundamentalType.LineBreak)
				{  iterator.Next();  }

				if (mode == ParseMode.SyntaxHighlight)
				{  iterator.Tokenizer.SetSyntaxHighlightingTypeBetween(startOfComment, iterator, SyntaxHighlightingType.Comment);  }

				return true;
			}
			else
			{  return false;  }
		}


		new protected bool TryToSkipBlockComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			if (iterator.MatchesAcrossTokens("/*"))
			{
				TokenIterator startOfComment = iterator;
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
					iterator.MatchesAcrossTokens("*/") == false)
				{  iterator.Next();  }

				if (iterator.MatchesAcrossTokens("*/"))
				{  iterator.NextByCharacters(2);  }

				if (mode == ParseMode.SyntaxHighlight)
				{  iterator.Tokenizer.SetSyntaxHighlightingTypeBetween(startOfComment, iterator, SyntaxHighlightingType.Comment);  }

				return true;
			}
			else
			{  return false;  }
		}


		protected void SkipRestOfStatement (ref TokenIterator iterator, bool angleBracketsAsBlocks = false)
		{
			while (iterator.IsInBounds)
			{
				if (iterator.Character == ';')
				{
					iterator.Next();
					break;
				}
				else if (iterator.Character == '{')
				{
					iterator.Next();
					GenericSkipUntilAfter(ref iterator, '}', angleBracketsAsBlocks);
					break;
				}
				else
				{  GenericSkip(ref iterator, angleBracketsAsBlocks);  }
			}
		}
		protected void GenericSkip (ref TokenIterator iterator, bool angleBracketsAsBlocks = false)
		{
			if (iterator.Character == '(')
			{  
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ')');  
			}
			else if (iterator.Character == '[')
			{  
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ']');  
			}
			else if (iterator.Character == '{')
			{  
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, '}');  
			}
			else if (iterator.Character == '<')
			{
				iterator.Next();

				if (angleBracketsAsBlocks)
				{  GenericSkipUntilAfter(ref iterator, '>', true);  }
			}

			else if (TryToSkipString(ref iterator) ||
				TryToSkipWhitespace(ref iterator) /*||
				TryToSkipPreprocessingDirective(ref iterator)*/)
				// Attributes are covered by the opening bracket
			{  }

			else
			{  iterator.Next();  }
		}

		protected void GenericSkipUntilAfter (ref TokenIterator iterator, char symbol, bool angleBracketsAsBlocks = false)
		{
			while (iterator.IsInBounds)
			{
				if (iterator.Character == symbol)
				{
					iterator.Next();
					break;
				}
				else
				{  GenericSkip(ref iterator, angleBracketsAsBlocks);  }
			}
		}
		override public ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
		{
			if (EngineInstance.CommentTypes.FromID(commentTypeID).Flags.ClassHierarchy == false)
			{  return null;  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);
			bool parsed = false;

			if (commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("class") ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("struct") ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("interface"))
			{
				parsed = TryToSkipClass(ref startOfPrototype, ParseMode.ParseClassPrototype);
			}

			if (parsed)
			{  return parsedPrototype;  }
			else
			{  return base.ParseClassPrototype(stringPrototype, commentTypeID);  }
		}

		protected bool TryToSkipClass (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null, 
			SymbolString scope = default(SymbolString))
		{
			// Classes - See [10] and [B.2.7]
			// Structs - See [11] and [B.2.8]
			// Interfaces - See [13] and [B.2.10]

			// While there are differences in the syntax of the three (classes have more possible modifiers, structs and interfaces can
			// only inherit interfaces, etc.) they are pretty small and for our purposes we can combine them into one parsing function.
			// It's okay to be over tolerant.

			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
			{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode))
			{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			TokenIterator startOfModifiers = lookahead;
			TokenIterator endOfModifiers = lookahead;

			AccessLevel accessLevel;

			// This covers "partial" as well, even though that's listed separately in the documentaton.
			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
			{  
				endOfModifiers = lookahead;
				TryToSkipWhitespace(ref lookahead);  
			}

			if (mode == ParseMode.ParseClassPrototype && 
				endOfModifiers > startOfModifiers)
			{  iterator.Tokenizer.SetClassPrototypeParsingTypeBetween(startOfModifiers, endOfModifiers, ClassPrototypeParsingType.Modifier);  }


			// Keyword

			if (lookahead.MatchesToken("class") == false &&
				lookahead.MatchesToken("struct") == false &&
				lookahead.MatchesToken("interface") == false)
			{  
				ResetTokensBetween(iterator, lookahead, mode);
				return false;  
			}

			string keyword = lookahead.String;

			if (mode == ParseMode.ParseClassPrototype)
			{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			string name;
			if (TryToSkipIdentifier(ref lookahead, out name, mode, PrototypeParsingType.Name) == false)
			{  
				ResetTokensBetween(iterator, lookahead, mode);
				return false;  
			}

			TryToSkipWhitespace(ref lookahead);


			// Template signature

			if (TryToSkipTemplateSignature(ref lookahead, mode, false))
			{  TryToSkipWhitespace(ref lookahead);  }


			// Base classes and interfaces

			if (lookahead.Character == ':')
			{
				if (mode == ParseMode.ParseClassPrototype)
				{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (;;)
				{
					TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Null);
					TryToSkipWhitespace(ref lookahead);

					if (TryToSkipTemplateSignature(ref lookahead, mode, true))
					{  TryToSkipWhitespace(ref lookahead);  }

					if (lookahead.Character != ',')
					{  break;  }

					if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
				}
			}


			// Constraint clauses

			while (TryToSkipWhereClause(ref lookahead, mode))
			{  TryToSkipWhitespace(ref lookahead);  }


			// Start of body

			if (lookahead.Character != '{' &&
				lookahead.IsInBounds)
			{  
				ResetTokensBetween(iterator, lookahead, mode);
				return false;  
			}


			// Create element

			if (mode == ParseMode.CreateElements)
			{
				SymbolString symbol = scope + SymbolString.FromPlainText_NoParameters(name);

				ClassString classString = ClassString.FromParameters(ClassString.HierarchyType.Class, this.ID, true, symbol);

				ContextString childContext = new ContextString();
				childContext.Scope = symbol;

				ParentElement classElement = new ParentElement(iterator, Element.Flags.InCode);
				classElement.DefaultChildLanguageID = this.ID;
				classElement.DefaultChildClassString = classString;
				classElement.ChildContextString = childContext;
				classElement.MaximumEffectiveChildAccessLevel = accessLevel;

				if (keyword == "interface")
				{  classElement.DefaultDeclaredChildAccessLevel = AccessLevel.Public;  }
				else // "class" or "struct"
				{  classElement.DefaultDeclaredChildAccessLevel = AccessLevel.Private;  }

				int commentTypeID = EngineInstance.CommentTypes.IDFromKeyword(keyword);

				if (commentTypeID != 0)
				{
					Topic classTopic = new Topic(EngineInstance.CommentTypes);
					classTopic.Title = symbol.FormatWithSeparator('.');  // so the title is fully resolved
					classTopic.Symbol = symbol;
					classTopic.ClassString = classString;
					classTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, lookahead) );
					classTopic.CommentTypeID = commentTypeID;
					classTopic.LanguageID = this.ID;
					classTopic.DeclaredAccessLevel = accessLevel;
					classTopic.CodeLineNumber = iterator.LineNumber;

					classElement.Topic = classTopic;
				}

				elements.Add(classElement);


				// Body

				iterator = lookahead;

				if (iterator.Character == '{')
				{
					iterator.Next();
					GetCodeElements(ref iterator, elements, symbol, '}');
				}

				classElement.EndingLineNumber = iterator.LineNumber;
				classElement.EndingCharNumber = iterator.CharNumber;

				return true;
			}

			else // mode isn't CreateElements
			{
				iterator = lookahead;

				if (iterator.Character == '{')
				{
					if (mode == ParseMode.ParseClassPrototype)
					{  iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfBody;  }

					iterator.Next();
					GenericSkipUntilAfter(ref iterator, '}');
				}

				return true;
			}
		}
		protected bool TryToSkipSliceDescription (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null, 
			SymbolString scope = default(SymbolString))
		{
			TokenIterator lookahead = iterator;
			if(lookahead.Character == '[')
			{
				lookahead.Next();
			}
			else
			{
				return false;
			}
		}
	}



}


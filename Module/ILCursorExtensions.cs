﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Celeste.Mod;

// since this is a legacy mod, i need to backport this class from Everest
namespace ExtendedVariants.Module {
    public static class ILCursorExtensions {
        public const int DefaultMaxInstructionSpread = 0x10;

        private const string NextBestFitLogID = $"{nameof(ILCursorExtensions)}/NextBestFit";
        private const string PrevBestFitLogID = $"{nameof(ILCursorExtensions)}/PrevBestFit";

        private struct Match {
            public int Start;
            public int End;

            public Match(int start, int end) {
                Start = start;
                End = end;
            }
        }

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <see cref="DefaultMaxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        public static void GotoNextBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates) {
            cursor.GotoNextBestFit(moveType, DefaultMaxInstructionSpread, predicates);
        }

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        public static void GotoNextBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates) {
            if (!cursor.TryGotoNextBestFit(moveType, maxInstructionSpread, predicates))
                throw new KeyNotFoundException($"Could not find a matching set of instructions with instruction spread of {maxInstructionSpread}.");
        }

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <c>0x10</c>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoNextBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates) {
            return TryGotoNextBestFit(cursor, moveType, DefaultMaxInstructionSpread, predicates);
        }

        /// <summary>
        ///   Go to the next best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the next match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoNextBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates) {
            if (predicates.Length == 0)
                throw new ArgumentException("No predicates given.");

            if (predicates.Length == 1)
                return cursor.TryGotoNext(moveType, predicates[0]);

            Logger.Log(LogLevel.Debug, NextBestFitLogID, $"Looking for next best fit in {cursor.Context.Method.FullName}.");
            Logger.Log(LogLevel.Debug, NextBestFitLogID, $"{nameof(ILCursor)}#{cursor.GetHashCode():X8} has initial index 0x{cursor.Index:X4}.");

            List<Match> matchCandidates = new();

            int initialPosition = cursor.Index;

            // go to each instance of the first predicate
            while (cursor.TryGotoNext(MoveType.Before, predicates[0])) {
                // remember where we were and move past the match
                int savedCursorPosition = cursor.Index++;

                // then try to match the rest of the predicates
                bool matchFound = true;
                for (int i = 1; i < predicates.Length; i++) {
                    Func<Instruction, bool> matcher = predicates[i];
                    int beforeMoveIndex = cursor.Index;

                    if (!cursor.TryGotoNext(MoveType.After, matcher)) {
                        Logger.Log(LogLevel.Verbose, NextBestFitLogID,
                            $"Matched predicate #0 at index 0x{savedCursorPosition:X4}, but failed to match predicate #{i}. Continuing search.");

                        matchFound = false;
                        break;
                    }

                    // also make sure we haven't gone further than maxInstructionSpread
                    int instructionSpread = cursor.Index - beforeMoveIndex;
                    if (instructionSpread > maxInstructionSpread) {
                        Logger.Log(LogLevel.Debug, NextBestFitLogID,
                            $"Matched predicate #0 at index 0x{savedCursorPosition:X4}, but the instruction spread between predicates #{i-1} and #{i} has been exceeded " +
                            $"({instructionSpread} > {maxInstructionSpread}). Continuing search.");

                        matchFound = false;
                        break;
                    }
                }

                if (matchFound) {
                    Logger.Log(LogLevel.Verbose, NextBestFitLogID,
                        $"Found match between indices 0x{savedCursorPosition:X4} and 0x{cursor.Index:X4}. Continuing search.");

                    // remember the start and end indices of the match
                    matchCandidates.Add(new Match(savedCursorPosition, cursor.Index));
                }

                // we go again
                // skip the first instance, else we'll get stuck
                cursor.Index = savedCursorPosition + 1;
            }

            // put the cursor back to where it was
            cursor.Index = initialPosition;

            if (matchCandidates.Count == 0) {
                // no match :c
                Logger.Log(LogLevel.Debug, NextBestFitLogID, $"Could not find next best fit for cursor {nameof(ILCursor)}#{cursor.GetHashCode():X8}.");
                return false;
            }

            // we found a match!
            // pick the one which has the least instruction spread

            Match bestMatch = matchCandidates[0];
            matchCandidates.RemoveAt(0);
            int bestMatchDiff = bestMatch.End - bestMatch.Start;

            foreach (Match matchCandidate in matchCandidates) {
                int matchCandidateDiff = matchCandidate.End - matchCandidate.Start;

                switch (matchCandidateDiff - bestMatchDiff) {
                    // we found a new best candidate
                    case < 0:

                    // we found two identical matches; pick the nearest one
                    case 0 when GetIndexFromMatch(moveType, matchCandidate) - cursor.Index < GetIndexFromMatch(moveType, bestMatch) - cursor.Index:
                        bestMatch = matchCandidate;
                        bestMatchDiff = matchCandidateDiff;
                        break;
                }
            }

            Logger.Log(LogLevel.Debug, NextBestFitLogID,
                $"Selecting next best fit between indices 0x{bestMatch.Start:X4} and 0x{bestMatch.End:X4} for cursor {nameof(ILCursor)}#{cursor.GetHashCode():X8}.");

            // some predicates may be using out parameters; invoke the predicates again to make sure
            // that the out parameters are set to ones from the match
            cursor.Index = bestMatch.Start;
            foreach (Func<Instruction, bool> predicate in predicates)
                cursor.GotoNext(predicate);

            // and finally move the cursor to the correct place
            cursor.Index = GetIndexFromMatch(moveType, bestMatch);

            if (moveType is MoveType.AfterLabel)
                cursor.MoveAfterLabels();

            return true;
        }

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <see cref="DefaultMaxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        public static void GotoPrevBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates) {
            cursor.GotoPrevBestFit(moveType, DefaultMaxInstructionSpread, predicates);
        }

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <exception cref="KeyNotFoundException">
        ///   A match could not be found.
        /// </exception>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        public static void GotoPrevBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates) {
            if (!cursor.TryGotoPrevBestFit(moveType, maxInstructionSpread, predicates))
                throw new KeyNotFoundException($"Could not find a matching set of instructions with instruction spread of {maxInstructionSpread}.");
        }

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <c>0x10</c>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoPrevBestFit(this ILCursor cursor, MoveType moveType, params Func<Instruction, bool>[] predicates) {
            return TryGotoPrevBestFit(cursor, moveType, DefaultMaxInstructionSpread, predicates);
        }

        /// <summary>
        ///   Go to the previous best fit match of a given IL sequence, allowing up to <paramref name="maxInstructionSpread"/>
        ///   instructions of tolerance if the instructions are not sequential.<br/>
        ///   (i.e. if something else hooks the same sequence)
        /// </summary>
        ///
        /// <param name="cursor">
        ///   The IL cursor to look for a match in.
        /// </param>
        /// <param name="moveType">
        ///   The move type to use.
        /// </param>
        /// <param name="maxInstructionSpread">
        ///   The amount of instructions between predicate matches to still consider as a successful match.
        /// </param>
        /// <param name="predicates">
        ///   The IL instructions to match against.
        /// </param>
        ///
        /// <remarks>
        ///   This function picks the previous match with the least instruction spread.<br/>
        ///
        ///   If there are two matches which have the same spread, pick the closest one.<br/>
        ///
        ///   If <paramref name="moveType"/> is set to <see cref="MoveType.After"/>, the resulting index
        ///   <b>may</b> have moved forwards instead of backwards, if the first predicate matches close to the cursor,
        ///   and the <paramref name="predicates"/> list is long enough.
        /// </remarks>
        ///
        /// <returns>
        ///   Whether a match has been found, and the cursor has been moved.
        /// </returns>
        public static bool TryGotoPrevBestFit(this ILCursor cursor, MoveType moveType, int maxInstructionSpread, params Func<Instruction, bool>[] predicates) {
            if (predicates.Length == 0)
                throw new ArgumentException("No predicates given.");

            if (predicates.Length == 1)
                return cursor.TryGotoNext(moveType, predicates[0]);

            Logger.Log(LogLevel.Debug, PrevBestFitLogID, $"Looking for previous best fit in {cursor.Context.Method.FullName}.");
            Logger.Log(LogLevel.Debug, PrevBestFitLogID, $"{nameof(ILCursor)}#{cursor.GetHashCode():X8} has initial index 0x{cursor.Index:X4}.");

            List<Match> matchCandidates = new();

            int initialPosition = cursor.Index;

            // go to each instance of the first predicate
            while (cursor.TryGotoPrev(MoveType.Before, predicates[0])) {
                // remember where we were and move past the match
                int savedCursorPosition = cursor.Index++;

                // then try to match the rest of the predicates
                bool matchFound = true;
                for (int i = 1; i < predicates.Length; i++) {
                    Func<Instruction, bool> matcher = predicates[i];
                    int beforeMoveIndex = cursor.Index;

                    if (!cursor.TryGotoNext(MoveType.After, matcher)) {
                        Logger.Log(LogLevel.Verbose, PrevBestFitLogID,
                            $"Matched predicate #0 at index 0x{savedCursorPosition:X4}, but failed to match predicate #{i}. Continuing search.");

                        matchFound = false;
                        break;
                    }

                    // also make sure we haven't gone further than maxInstructionSpread
                    // note that we could have gone past the initial cursor position!
                    int instructionSpread = cursor.Index - beforeMoveIndex;
                    if (instructionSpread > maxInstructionSpread) {
                        Logger.Log(LogLevel.Debug, PrevBestFitLogID,
                            $"Matched predicate #0 at index 0x{savedCursorPosition:X4}, but the instruction spread between predicates #{i-1} and #{i} has been exceeded " +
                            $"({instructionSpread} > {maxInstructionSpread}). Continuing search.");

                        matchFound = false;
                        break;
                    }
                }

                if (matchFound) {
                    Logger.Log(LogLevel.Verbose, PrevBestFitLogID,
                        $"Found match between indices 0x{savedCursorPosition:X4} and 0x{cursor.Index:X4}. Continuing search.");

                    // remember the start and end indices of the match
                    matchCandidates.Add(new Match(savedCursorPosition, cursor.Index));
                }

                // we go again
                cursor.Index = savedCursorPosition;
            }

            // put the cursor back to where it was
            cursor.Index = initialPosition;

            if (matchCandidates.Count == 0) {
                // no match :c
                Logger.Log(LogLevel.Debug, PrevBestFitLogID, $"Could not find previous best fit for cursor {nameof(ILCursor)}#{cursor.GetHashCode():X8}.");
                return false;
            }

            // we found a match!
            // pick the one which has the least instruction spread

            Match bestMatch = matchCandidates[0];
            matchCandidates.RemoveAt(0);
            int bestMatchDiff = bestMatch.End - bestMatch.Start;

            foreach (Match matchCandidate in matchCandidates) {
                int matchCandidateDiff = matchCandidate.End - matchCandidate.Start;

                switch (matchCandidateDiff - bestMatchDiff) {
                    // we found a new best candidate
                    case < 0:

                    // we found two identical matches; pick the nearest one
                    case 0 when cursor.Index - GetIndexFromMatch(moveType, matchCandidate) < cursor.Index - GetIndexFromMatch(moveType, bestMatch):
                        bestMatch = matchCandidate;
                        bestMatchDiff = matchCandidateDiff;
                        break;
                }
            }

            Logger.Log(LogLevel.Debug, PrevBestFitLogID,
                $"Selecting previous best fit between indices 0x{bestMatch.Start:X4} and 0x{bestMatch.End:X4} for cursor {nameof(ILCursor)}#{cursor.GetHashCode():X8}.");

            // some predicates may be using out parameters; invoke them again to make sure
            // that the out parameters are set to ones from the match
            cursor.Index = bestMatch.Start;
            foreach (Func<Instruction, bool> predicate in predicates)
                cursor.GotoNext(predicate);

            // and finally move the cursor to the correct place
            cursor.Index = GetIndexFromMatch(moveType, bestMatch);

            if (moveType is MoveType.AfterLabel)
                cursor.MoveAfterLabels();

            return true;
        }

        private static int GetIndexFromMatch(MoveType moveType, Match match) {
            return moveType == MoveType.After ? match.End : match.Start;
        }
    }
}

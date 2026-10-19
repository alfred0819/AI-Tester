#!/usr/bin/env python3
"""Mirrors Aabb2.overlaps / flipped logic for CI without Godot."""

from __future__ import annotations


class Aabb2:
    def __init__(self, x: float, y: float, w: float, h: float) -> None:
        self.x, self.y, self.w, self.h = x, y, w, h

    def overlaps(self, other: "Aabb2") -> bool:
        return (
            self.x < other.x + other.w
            and self.x + self.w > other.x
            and self.y < other.y + other.h
            and self.y + self.h > other.y
        )

    def flipped(self, facing: int) -> "Aabb2":
        if facing >= 0:
            return Aabb2(self.x, self.y, self.w, self.h)
        return Aabb2(-(self.x + self.w), self.y, self.w, self.h)


def test_overlaps() -> None:
    a = Aabb2(0, 0, 10, 10)
    b = Aabb2(5, 5, 10, 10)
    c = Aabb2(20, 20, 5, 5)
    assert a.overlaps(b)
    assert not a.overlaps(c)


def test_flip() -> None:
    local = Aabb2(10, -20, 30, 40)
    f = local.flipped(-1)
    assert f.x == -40
    assert f.w == 30


def test_dir_table() -> None:
    """Sanity for numpad mapping used by ActionSampler."""

    def dir_from_axes(x: int, y: int) -> int:
        table = {
            (-1, 1): 7,
            (0, 1): 8,
            (1, 1): 9,
            (-1, 0): 4,
            (0, 0): 5,
            (1, 0): 6,
            (-1, -1): 1,
            (0, -1): 2,
            (1, -1): 3,
        }
        return table[(x, y)]

    assert dir_from_axes(0, 0) == 5
    assert dir_from_axes(1, 0) == 6
    assert dir_from_axes(-1, 1) == 7


if __name__ == "__main__":
    test_overlaps()
    test_flip()
    test_dir_table()
    print("OK: test_aabb2.py")

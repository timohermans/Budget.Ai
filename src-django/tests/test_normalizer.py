from __future__ import annotations

from django.test import SimpleTestCase

from apps.merchants.normalizer import normalize


class MerchantNameNormalizerTests(SimpleTestCase):
    def test_normalize_when_case_differs_then_same_key(self) -> None:
        self.assertEqual(normalize("Albert Heijn"), normalize("ALBERT HEIJN"))

    def test_normalize_when_whitespace_differs_then_same_key(self) -> None:
        self.assertEqual(normalize("Albert  Heijn"), normalize("  Albert Heijn "))

    def test_normalize_when_hyphen_spacing_differs_then_same_key(self) -> None:
        self.assertEqual(
            normalize("AH- Jan Linders 4181"),
            normalize("AH - Jan Linders 4181"),
        )

    def test_normalize_when_hyphen_adjacent_then_key_has_spaced_hyphen(self) -> None:
        self.assertEqual("ah - jan linders 4181", normalize("AH - Jan Linders 4181"))

    def test_normalize_when_blank_then_empty(self) -> None:
        self.assertEqual("", normalize(None))
        self.assertEqual("", normalize("   "))

    def test_normalize_when_merchant_example_then_stable_key(self) -> None:
        variants = [
            "AH- Jan Linders 4181",
            "AH - Jan Linders 4149",
            "Albert Heijn Online",
            "AH Grevenbicht",
            "Albert Heijn 1194",
            "Albert Heijn 1517",
            "Albert Heijn",
        ]
        keys = {normalize(value) for value in variants}
        self.assertIn("albert heijn", keys)
        self.assertTrue(all(keys))

"""Annotate data/faqs-ec-website.json with question_category_values.

Run from the repo root:
    python scripts/annotate-faq-categories.py
"""

import json
import os
import sys

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FAQ_PATH = os.path.join(REPO_ROOT, "data", "faqs-ec-website.json")

ALL_TICKET = ["Standard", "Vip", "UltraVip", "Black"]
ALL_ACCOMMODATION = ["VillageRental", "Camping", "CarCamping", "RvCamping", "Glamping"]
ALL_TRANSPORT = ["RideShare", "Car", "EcTrain", "EcBus", "Helicopter"]
ALL_FOOD = [
    "Vegan", "Vegetarian", "NoPeanuts", "NoMeat", "NoPork",
    "NoDairy", "NoGluten", "NoShellfish", "NoEggs", "Halal", "Kosher",
]
CAMPING_ONLY = ["Camping", "CarCamping", "RvCamping"]

SECTION_DEFAULTS = {
    "Tickets":              {"Ticket": ALL_TICKET},
    "Merchandise":          {"Ticket": ALL_TICKET},
    "Exchange Platform":    {"Ticket": ALL_TICKET},
    "Festival Area":        {"Ticket": ALL_TICKET},
    "Cashless System":      {"Ticket": ALL_TICKET},
    "EC Village":           {"Accommodation": ALL_ACCOMMODATION},
    "Transportation":       {"Transport": ALL_TRANSPORT},
    "Vendors & Volunteers": {"Ticket": ALL_TICKET},
}

# Per-source_ref overrides (takes full precedence over section default).
# Duplicate source_refs are handled separately via answer text matching.
SOURCE_REF_OVERRIDES = {
    # Festival Area specific
    "ec-website/faq/festival-area/what-can-i-eat-and-drink":
        {"Ticket": ALL_TICKET, "Food": ALL_FOOD},
    "ec-website/faq/festival-area/can-i-bring-food-or-beverages-with-me":
        {"Ticket": ALL_TICKET, "Food": ALL_FOOD},
    "ec-website/faq/festival-area/can-enter-the-event-grounds-by-car":
        {"Ticket": ALL_TICKET, "Transport": ["Car"]},
    "ec-website/faq/festival-area/how-do-i-get-around":
        {"Ticket": ALL_TICKET, "Transport": ALL_TRANSPORT},

    # Transportation per-mode
    "ec-website/faq/transportation/is-there-a-bus-from-cluj-napoca-to-bonida":
        {"Transport": ["EcBus"]},
    "ec-website/faq/transportation/what-is-the-schedule":
        {"Transport": ["EcBus"]},
    "ec-website/faq/transportation/how-can-i-buy-bus-tickets":
        {"Transport": ["EcBus"]},
    "ec-website/faq/transportation/can-i-get-there-by-car":
        {"Transport": ["Car"]},
    "ec-website/faq/transportation/where-do-i-park":
        {"Transport": ["Car"]},
    "ec-website/faq/transportation/whats-the-price-of-the-parking":
        {"Transport": ["Car"]},
    # bike and plane not in TransportMode enum → broad (ticket + all transport)
    "ec-website/faq/transportation/can-i-come-by-bike":
        {"Ticket": ALL_TICKET, "Transport": ALL_TRANSPORT},
    "ec-website/faq/transportation/can-i-come-by-train":
        {"Transport": ["EcTrain"]},
    "ec-website/faq/transportation/can-i-come-by-plane":
        {"Ticket": ALL_TICKET, "Transport": ALL_TRANSPORT},

    # EC Village – glamping-specific
    "ec-website/faq/ec-village/what-does-glamping-mean":
        {"Accommodation": ["Glamping"]},
    "ec-website/faq/ec-village/how-much-do-the-glamping-accommodation-options-cost":
        {"Accommodation": ["Glamping"]},

    # EC Village – car access
    "ec-website/faq/ec-village/can-i-enter-with-a-car-in-the-ec-village":
        {"Accommodation": ["CarCamping", "RvCamping"], "Transport": ["Car"]},

    # EC Village – fire/generator (camping-only)
    "ec-website/faq/ec-village/can-i-set-a-fire":
        {"Accommodation": CAMPING_ONLY},
    "ec-website/faq/ec-village/can-bring-an-electricity-generator":
        {"Accommodation": CAMPING_ONLY},

    # EC Village – car camping section
    "ec-website/faq/ec-village/what-does-car-camping-mean":
        {"Accommodation": ["CarCamping"]},
    "ec-website/faq/ec-village/how-can-i-come-with-my-car":
        {"Accommodation": ["CarCamping"], "Transport": ["Car"]},
    "ec-website/faq/ec-village/can-i-park-my-rv-in-the-car-camping":
        {"Accommodation": ["CarCamping", "RvCamping"]},
    "ec-website/faq/ec-village/if-i-have-a-car-camping-ticket-can-i-enter-the-ec-village-areas":
        {"Accommodation": ["CarCamping"]},
    "ec-website/faq/ec-village/can-i-leave-from-the-car-camping-in-case-of-an-emergency":
        {"Accommodation": ["CarCamping"]},

    # EC Village – RV camping section
    "ec-website/faq/ec-village/can-i-bring-an-electricity-generator-and-other-appliances":
        {"Accommodation": ["RvCamping"]},
    "ec-website/faq/ec-village/what-does-rv-camping-mean":
        {"Accommodation": ["RvCamping"]},
    "ec-website/faq/ec-village/how-can-i-come-with-my-caravan-to-the-festival":
        {"Accommodation": ["RvCamping"], "Transport": ["Car"]},
    "ec-website/faq/ec-village/can-i-park-my-car-in-the-rv-area":
        {"Accommodation": ["RvCamping"], "Transport": ["Car"]},
    "ec-website/faq/ec-village/do-i-have-a-water-source":
        {"Accommodation": ["RvCamping"]},
    "ec-website/faq/ec-village/if-i-have-an-rv-ticket-can-i-enter-the-camping-areas":
        {"Accommodation": ["RvCamping"]},
}

# Duplicate source_ref – resolved by matching a keyword in the first answer text.
DUPLICATE_ANSWER_OVERRIDES = {
    "ec-website/faq/ec-village/can-i-have-a-side-tentterrace": [
        # (answer_keyword, category_values)
        ("car", {"Accommodation": ["CarCamping"]}),
        ("rv",  {"Accommodation": ["RvCamping"]}),
    ],
}


def resolve(entry: dict) -> dict:
    ref = entry["source_ref"]
    section = entry["section"]

    if ref in DUPLICATE_ANSWER_OVERRIDES:
        first_answer = entry["answers"][0]["text"].lower() if entry["answers"] else ""
        for keyword, cat_values in DUPLICATE_ANSWER_OVERRIDES[ref]:
            if keyword in first_answer:
                return cat_values
        # fallback: section default
        return SECTION_DEFAULTS.get(section, {})

    if ref in SOURCE_REF_OVERRIDES:
        return SOURCE_REF_OVERRIDES[ref]

    return SECTION_DEFAULTS.get(section, {})


def main():
    with open(FAQ_PATH, encoding="utf-8") as f:
        data = json.load(f)

    changed = 0
    for entry in data:
        cat = resolve(entry)
        if entry.get("question_category_values") != cat:
            entry["question_category_values"] = cat
            changed += 1

    with open(FAQ_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print(f"Updated {changed}/{len(data)} entries → {FAQ_PATH}")


if __name__ == "__main__":
    main()

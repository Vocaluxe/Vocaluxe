#include "stdafx.h"
#include "VocaluxeClock.h"

static void vocaluxe_clock_class_init (VocaluxeClockClass * klass);
static void vocaluxe_clock_init (VocaluxeClock * clock);

static GstClockTime vocaluxe_clock_get_internal_time (GstClock * clock);

static GstSystemClockClass *parent_class = NULL;

GType
vocaluxe_clock_get_type (void)
{
  static GType clock_type = 0;

  if (!clock_type) {
    static const GTypeInfo clock_info = {
      sizeof (VocaluxeClockClass),
      NULL,
      NULL,
      (GClassInitFunc) vocaluxe_clock_class_init,
      NULL,
      NULL,
      sizeof (VocaluxeClock),
      4,
      (GInstanceInitFunc) vocaluxe_clock_init,
      NULL
    };

    clock_type = g_type_register_static (GST_TYPE_SYSTEM_CLOCK, "VocaluxeClock",
        &clock_info, (GTypeFlags) 0);
  }
  return clock_type;
}


static void
vocaluxe_clock_class_init (VocaluxeClockClass * klass)
{
  GstClockClass *gstclock_class;

  gstclock_class = (GstClockClass *) klass;

  parent_class = (GstSystemClockClass*) g_type_class_peek_parent (klass);

  gstclock_class->get_internal_time = vocaluxe_clock_get_internal_time;
}

static void
vocaluxe_clock_init (VocaluxeClock * clock)
{
  clock->last_time = 0;
  GST_OBJECT_FLAG_SET (clock, GST_CLOCK_FLAG_CAN_SET_MASTER);
}

VocaluxeClock *
vocaluxe_clock_new (const gchar * name)
{
  VocaluxeClock *_vocaluxeclock =
      VOCALUXE_CLOCK (g_object_new (GST_TYPE_VOCALUXE_CLOCK, "name", name, NULL));

  return _vocaluxeclock;
}

static GstClockTime
vocaluxe_clock_get_internal_time (GstClock * clock)
{
  VocaluxeClock *_vocaluxeclock;
  GstClockTime result;

  _vocaluxeclock = GST_VOCALUXE_CLOCK_CAST (clock);

  result = (GstClockTime) _vocaluxeclock->time * GST_SECOND;
  return result;
}

void
vocaluxe_clock_set_time (VocaluxeClock * clock, float time)
{
  clock->time = time;
}
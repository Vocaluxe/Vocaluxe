#pragma once

#include <gst/gst.h>
#include <gst/gstsystemclock.h>

G_BEGIN_DECLS

#define GST_TYPE_VOCALUXE_CLOCK \
  (vocaluxe_clock_get_type())
#define VOCALUXE_CLOCK(obj) \
  (G_TYPE_CHECK_INSTANCE_CAST((obj),GST_TYPE_VOCALUXE_CLOCK,VocaluxeClock))
#define VOCALUXE_CLOCK_CLASS(klass) \
  (G_TYPE_CHECK_CLASS_CAST((klass),GST_TYPE_VOCALUXE_CLOCK,VocaluxeClockClass))
#define GST_IS_VOCALUXE_CLOCK(obj) \
  (G_TYPE_CHECK_INSTANCE_TYPE((obj),GST_TYPE_VOCALUXE_CLOCK))
#define GST_IS_VOCALUXE_CLOCK_CLASS(klass) \
  (G_TYPE_CHECK_CLASS_TYPE((klass),GST_TYPE_VOCALUXE_CLOCK))
#define GST_VOCALUXE_CLOCK_CAST(obj) \
  ((VocaluxeClock*)(obj))

typedef struct _VocaluxeClock VocaluxeClock;
typedef struct _VocaluxeClockClass VocaluxeClockClass;

struct _VocaluxeClock {
  GstSystemClock clock;

  GstClockTime last_time;
  gfloat time;
};

struct _VocaluxeClockClass {
  GstSystemClockClass parent_class;

  /*< private >*/
  gpointer _gst_reserved[GST_PADDING];
};

GType          vocaluxe_clock_get_type        (void);
VocaluxeClock*   vocaluxe_clock_new             (const gchar *name);
void            vocaluxe_clock_set_time      (VocaluxeClock *clock, float time);

}
/*
 * Glue library for gstreamer-sharp (libgstreamersharpglue-1.0.0) on macOS/Linux.
 * The Windows version ships as DLL in the NuGet package vocaluxe.dependencies.gstreamersharp.
 * gstreamer-sharp asks it for struct sizes and field offsets of the GStreamer/GLib headers.
 * The list of functions matches the imports of gstreamer-sharp.dll.
 */
#include <glib-object.h>
#include <gst/gst.h>
#include <gst/base/base.h>
#include <gst/controller/controller.h>

gint gstsharp_g_closure_sizeof (void) { return (gint) sizeof (GClosure); }
GType gstsharp_g_type_from_instance (GTypeInstance *instance) { return G_TYPE_FROM_INSTANCE (instance); }

guint gstsharp_gst_allocator_get_mem_copy_offset (void) { return (guint) G_STRUCT_OFFSET (GstAllocator, mem_copy); }
guint gstsharp_gst_allocator_get_mem_is_span_offset (void) { return (guint) G_STRUCT_OFFSET (GstAllocator, mem_is_span); }
guint gstsharp_gst_allocator_get_mem_map_offset (void) { return (guint) G_STRUCT_OFFSET (GstAllocator, mem_map); }
guint gstsharp_gst_allocator_get_mem_share_offset (void) { return (guint) G_STRUCT_OFFSET (GstAllocator, mem_share); }
guint gstsharp_gst_allocator_get_mem_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstAllocator, mem_type); }
guint gstsharp_gst_allocator_get_mem_unmap_offset (void) { return (guint) G_STRUCT_OFFSET (GstAllocator, mem_unmap); }
guint gstsharp_gst_bin_get_child_bus_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, child_bus); }
guint gstsharp_gst_bin_get_children_cookie_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, children_cookie); }
guint gstsharp_gst_bin_get_children_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, children); }
guint gstsharp_gst_bin_get_clock_dirty_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, clock_dirty); }
guint gstsharp_gst_bin_get_clock_provider_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, clock_provider); }
guint gstsharp_gst_bin_get_messages_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, messages); }
guint gstsharp_gst_bin_get_numchildren_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, numchildren); }
guint gstsharp_gst_bin_get_polling_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, polling); }
guint gstsharp_gst_bin_get_provided_clock_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, provided_clock); }
guint gstsharp_gst_bin_get_state_dirty_offset (void) { return (guint) G_STRUCT_OFFSET (GstBin, state_dirty); }
guint gstsharp_gst_buffer_get_dts_offset (void) { return (guint) G_STRUCT_OFFSET (GstBuffer, dts); }
guint gstsharp_gst_buffer_get_duration_offset (void) { return (guint) G_STRUCT_OFFSET (GstBuffer, duration); }
guint gstsharp_gst_buffer_get_offset_end_offset (void) { return (guint) G_STRUCT_OFFSET (GstBuffer, offset_end); }
guint gstsharp_gst_buffer_get_offset_offset (void) { return (guint) G_STRUCT_OFFSET (GstBuffer, offset); }
guint gstsharp_gst_buffer_get_pool_offset (void) { return (guint) G_STRUCT_OFFSET (GstBuffer, pool); }
guint gstsharp_gst_buffer_get_pts_offset (void) { return (guint) G_STRUCT_OFFSET (GstBuffer, pts); }
guint gstsharp_gst_bufferpool_get_flushing_offset (void) { return (guint) G_STRUCT_OFFSET (GstBufferPool, flushing); }
guint gstsharp_gst_clockentry_get_clock_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, clock); }
guint gstsharp_gst_clockentry_get_interval_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, interval); }
guint gstsharp_gst_clockentry_get_refcount_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, refcount); }
guint gstsharp_gst_clockentry_get_status_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, status); }
guint gstsharp_gst_clockentry_get_time_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, time); }
guint gstsharp_gst_clockentry_get_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, type); }
guint gstsharp_gst_clockentry_get_unscheduled_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, unscheduled); }
guint gstsharp_gst_clockentry_get_user_data_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, user_data); }
guint gstsharp_gst_clockentry_get_woken_up_offset (void) { return (guint) G_STRUCT_OFFSET (GstClockEntry, woken_up); }
guint gstsharp_gst_controlbinding_get_name_offset (void) { return (guint) G_STRUCT_OFFSET (GstControlBinding, name); }
guint gstsharp_gst_controlbinding_get_pspec_offset (void) { return (guint) G_STRUCT_OFFSET (GstControlBinding, pspec); }
guint gstsharp_gst_controlsource_get_get_value_array_offset (void) { return (guint) G_STRUCT_OFFSET (GstControlSource, get_value_array); }
guint gstsharp_gst_controlsource_get_get_value_offset (void) { return (guint) G_STRUCT_OFFSET (GstControlSource, get_value); }
guint gstsharp_gst_element_get_current_state_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, current_state); }
guint gstsharp_gst_element_get_last_return_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, last_return); }
guint gstsharp_gst_element_get_next_state_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, next_state); }
guint gstsharp_gst_element_get_numpads_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, numpads); }
guint gstsharp_gst_element_get_numsinkpads_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, numsinkpads); }
guint gstsharp_gst_element_get_numsrcpads_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, numsrcpads); }
guint gstsharp_gst_element_get_pads_cookie_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, pads_cookie); }
guint gstsharp_gst_element_get_pads_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, pads); }
guint gstsharp_gst_element_get_pending_state_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, pending_state); }
guint gstsharp_gst_element_get_sinkpads_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, sinkpads); }
guint gstsharp_gst_element_get_srcpads_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, srcpads); }
guint gstsharp_gst_element_get_state_cond_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, state_cond); }
guint gstsharp_gst_element_get_state_cookie_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, state_cookie); }
guint gstsharp_gst_element_get_state_lock_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, state_lock); }
guint gstsharp_gst_element_get_target_state_offset (void) { return (guint) G_STRUCT_OFFSET (GstElement, target_state); }
guint gstsharp_gst_event_get_timestamp_offset (void) { return (guint) G_STRUCT_OFFSET (GstEvent, timestamp); }
guint gstsharp_gst_event_get_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstEvent, type); }
guint gstsharp_gst_ghostpad_get_pad_offset (void) { return (guint) G_STRUCT_OFFSET (GstGhostPad, pad); }
guint gstsharp_gst_iterator_get_cookie_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, cookie); }
guint gstsharp_gst_iterator_get_copy_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, copy); }
guint gstsharp_gst_iterator_get_free_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, free); }
guint gstsharp_gst_iterator_get_item_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, item); }
guint gstsharp_gst_iterator_get_lock_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, lock); }
guint gstsharp_gst_iterator_get_master_cookie_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, master_cookie); }
guint gstsharp_gst_iterator_get_next_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, next); }
guint gstsharp_gst_iterator_get_pushed_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, pushed); }
guint gstsharp_gst_iterator_get_resync_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, resync); }
guint gstsharp_gst_iterator_get_size_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, size); }
guint gstsharp_gst_iterator_get_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstIterator, type); }
guint gstsharp_gst_memory_get_align_offset (void) { return (guint) G_STRUCT_OFFSET (GstMemory, align); }
guint gstsharp_gst_memory_get_allocator_offset (void) { return (guint) G_STRUCT_OFFSET (GstMemory, allocator); }
guint gstsharp_gst_memory_get_maxsize_offset (void) { return (guint) G_STRUCT_OFFSET (GstMemory, maxsize); }
guint gstsharp_gst_memory_get_offset_offset (void) { return (guint) G_STRUCT_OFFSET (GstMemory, offset); }
guint gstsharp_gst_memory_get_parent_offset (void) { return (guint) G_STRUCT_OFFSET (GstMemory, parent); }
guint gstsharp_gst_memory_get_size_offset (void) { return (guint) G_STRUCT_OFFSET (GstMemory, size); }
guint gstsharp_gst_message_get_src_offset (void) { return (guint) G_STRUCT_OFFSET (GstMessage, src); }
guint gstsharp_gst_message_get_timestamp_offset (void) { return (guint) G_STRUCT_OFFSET (GstMessage, timestamp); }
guint gstsharp_gst_message_get_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstMessage, type); }
guint gstsharp_gst_miniobject_get_copy_offset (void) { return (guint) G_STRUCT_OFFSET (GstMiniObject, copy); }
guint gstsharp_gst_miniobject_get_dispose_offset (void) { return (guint) G_STRUCT_OFFSET (GstMiniObject, dispose); }
guint gstsharp_gst_miniobject_get_flags_offset (void) { return (guint) G_STRUCT_OFFSET (GstMiniObject, flags); }
guint gstsharp_gst_miniobject_get_free_offset (void) { return (guint) G_STRUCT_OFFSET (GstMiniObject, free); }
guint gstsharp_gst_miniobject_get_lockstate_offset (void) { return (guint) G_STRUCT_OFFSET (GstMiniObject, lockstate); }
guint gstsharp_gst_miniobject_get_refcount_offset (void) { return (guint) G_STRUCT_OFFSET (GstMiniObject, refcount); }
guint gstsharp_gst_miniobject_get_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstMiniObject, type); }
guint gstsharp_gst_object_get_flags_offset (void) { return (guint) G_STRUCT_OFFSET (GstObject, flags); }
guint gstsharp_gst_object_get_lock_offset (void) { return (guint) G_STRUCT_OFFSET (GstObject, lock); }
guint gstsharp_gst_object_get_name_offset (void) { return (guint) G_STRUCT_OFFSET (GstObject, name); }
guint gstsharp_gst_pad_get_direction_offset (void) { return (guint) G_STRUCT_OFFSET (GstPad, direction); }
guint gstsharp_gst_pad_get_padtemplate_offset (void) { return (guint) G_STRUCT_OFFSET (GstPad, padtemplate); }
guint gstsharp_gst_padtemplate_get_caps_offset (void) { return (guint) G_STRUCT_OFFSET (GstPadTemplate, caps); }
guint gstsharp_gst_padtemplate_get_direction_offset (void) { return (guint) G_STRUCT_OFFSET (GstPadTemplate, direction); }
guint gstsharp_gst_padtemplate_get_name_template_offset (void) { return (guint) G_STRUCT_OFFSET (GstPadTemplate, name_template); }
guint gstsharp_gst_padtemplate_get_presence_offset (void) { return (guint) G_STRUCT_OFFSET (GstPadTemplate, presence); }
guint gstsharp_gst_pipeline_get_delay_offset (void) { return (guint) G_STRUCT_OFFSET (GstPipeline, delay); }
guint gstsharp_gst_pipeline_get_fixed_clock_offset (void) { return (guint) G_STRUCT_OFFSET (GstPipeline, fixed_clock); }
guint gstsharp_gst_pipeline_get_stream_time_offset (void) { return (guint) G_STRUCT_OFFSET (GstPipeline, stream_time); }
guint gstsharp_gst_proxypad_get_pad_offset (void) { return (guint) G_STRUCT_OFFSET (GstProxyPad, pad); }
guint gstsharp_gst_query_get_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstQuery, type); }
guint gstsharp_gst_structure_get_type_offset (void) { return (guint) G_STRUCT_OFFSET (GstStructure, type); }
guint gstsharp_gst_systemclock_get_clock_offset (void) { return (guint) G_STRUCT_OFFSET (GstSystemClock, clock); }
guint gstsharp_gst_task_get_cond_offset (void) { return (guint) G_STRUCT_OFFSET (GstTask, cond); }
guint gstsharp_gst_task_get_func_offset (void) { return (guint) G_STRUCT_OFFSET (GstTask, func); }
guint gstsharp_gst_task_get_lock_offset (void) { return (guint) G_STRUCT_OFFSET (GstTask, lock); }
guint gstsharp_gst_task_get_running_offset (void) { return (guint) G_STRUCT_OFFSET (GstTask, running); }
guint gstsharp_gst_task_get_user_data_offset (void) { return (guint) G_STRUCT_OFFSET (GstTask, user_data); }
